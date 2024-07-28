using MTG_Cards.Data;
using MTG_Cards.Models;
using MTG_Cards.DTOs;
using MTG_Cards.Services.Mappers;
using MTG_Cards.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using MTG_Cards.Services;

namespace MTG_Cards.Repositories
{
	public class UserRepository : IUserRepository
	{
		private readonly DataContext _context;
		private readonly IDistributedCache _distributedCache;
		private readonly ICacheHelper _cacheHelper;

		public UserRepository(DataContext context, IDistributedCache distributedCache, ICacheHelper cacheHelper)
		{
			_context = context;
			_distributedCache = distributedCache;
			_cacheHelper = cacheHelper;
		}

		public bool UserExists(string username)
		{
			return _context.Users.Any(u => u.Username == username);
		}

		public bool UserEmailExists(string email)
		{
			return _context.Users.Any(u => u.Email == email);
		}

		public User? GetUserById(int id)
		{
			return _context.Users.FirstOrDefault(u => u.Id == id);
		}

		public User? GetUserByUsername(string username) 
		{
			return _context.Users.FirstOrDefault(u => u.Username == username);
		}

		public async Task<User?> GetUserByEmail(string email)
		{
			return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
		}


		public async Task<CardPageDTO> GetCardsOwned(string username, int page, string? search, int? editionId, string? sortBy, string? foilFilter)
		{
			string key = GenerateCacheKey(username, page, search, editionId, sortBy, foilFilter);

			var cachedCards = await CacheHelper.GetCacheEntry<CardPageDTO?>(_distributedCache, key);

			if (cachedCards == null)
			{
				try
				{
					var query = ApplyCardFilters(username, search, editionId, sortBy, foilFilter);
					var numResults = await query.CountAsync();

					List<Card> cards = await query
						.Skip(page * 50)
						.Take(50)
						.ToListAsync();

					List<CardDTO> cardsDTO = cards.Select(card => CardMapper.ToDTO(card)).ToList();
					CardPageDTO cardPageDTO = new CardPageDTO(page + 1, (int)Math.Ceiling(numResults / 50.0), numResults, cardsDTO);

					await CacheHelper.SetCacheEntry(_distributedCache, key, cardPageDTO);

					return cardPageDTO;
				}
				catch (Exception ex)
				{
					// Log the exception
					Console.WriteLine($"Error in GetCardsOwned: {ex.Message}");
					throw; // Re-throw the exception after logging
				}
			}

			return cachedCards.Value;
		}

		private IQueryable<Card> ApplyCardFilters(string username, string? search, int? editionId, string? sortBy, string? foilFilter)
		{
			IQueryable<Card> query = _context.Users
				.Where(u => u.Username == username)
				.SelectMany(u => u.CardsOwned.Select(co => co.CardCondition!.Card!).Distinct())
				.Include(c => c.Edition)
				.AsNoTracking();

			if (!string.IsNullOrEmpty(search))
				query = query.Where(c => c.Name.ToLower().Contains(search.ToLower()));

			if (editionId.HasValue)
				query = query.Where(c => c.EditionId == editionId.Value);

			switch (foilFilter)
			{
				case "foils_only":
					query = query.Where(c => c.IsFoil);
					break;
				case "hide_foils":
					query = query.Where(c => !c.IsFoil);
					break;
				default:
					break;
			}

			switch (sortBy)
			{
				case "price_asc":
					query = query.OrderBy(c => c.NMPrice);
					break;
				case "price_desc":
					query = query.OrderByDescending(c => c.NMPrice);
					break;
				case "rarity_asc":
					query = query.OrderBy(c => c.Rarity);
					break;
				case "rarity_desc":
					query = query.OrderByDescending(c => c.Rarity);
					break;
				default:
					break;
			}

			return query;
		}

		public string GenerateCacheKey(string username, int page, string? search, int? editionId, string? sortBy, string? foilFilter)
		{
			return $"user_{username.ToLower()}_cards_page_{page}_search_{search ?? "none"}_edition_{editionId?.ToString() ?? "none"}_sort_{sortBy ?? "none"}_foilFilter_{foilFilter ?? "none"}";
		}

		public async Task<(int totalCards, double totalValue)> GetTotalCardsAndValue(string username)
		{
			try
			{
				var isValidUser = await _context.Users.AnyAsync(u => u.Username == username);
				if (!isValidUser)
					throw new Exception("User not found");

					var cardQuantities = await _context.Users
						.Where(u => u.Username == username)
						.Include(u => u.CardsOwned)
						.SelectMany(u => u.CardsOwned.Select(co => co.Quantity))
						.ToListAsync();

				var cardValues = await _context.Users
						.Where(u => u.Username == username)
						.SelectMany(u => u.CardsOwned.Select(co => co.CardCondition!.Price))
						.ToListAsync();

				int totalCards = cardQuantities.Sum();
				double totalValue = 0;

				for (int i = 0; i < cardQuantities.Count; i++)
					totalValue += cardQuantities[i] * cardValues[i];

				return (totalCards, totalValue);
			} catch (Exception ex)
			{
				Console.Write("An error occurred while trying to get total cards and value: ", ex);
				return (-1, -1);
			}
		}

		public bool LoginUser(UserLoginDTO userDTO)
		{
			User? user = _context.Users.FirstOrDefault(u => u.Username == userDTO.Username);
			if (user == null) return false;

			string hashedPassword = PasswordHashHelper.HashPassword(userDTO.Password, user.Salt);
			if (hashedPassword != user.Password) return false;

			return true;
		}

		public bool RegisterUser(UserLoginDTO userDTO)
		{
			var (hashedPassword, salt) = PasswordHashHelper.GenerateHashedPasswordAndSalt(userDTO.Password);
			userDTO.Password = hashedPassword;
			_context.Users.Add(UserMapper.ToModel(userDTO, salt));
			return Save();
		}

		public async Task<List<CardOwnedDTO>> GetCardConditions(string username, int cardId)
		{
			return await _context.CardsOwned
				.Include(co => co.User)
				.Where(co => co!.User!.Username == username && co!.CardCondition!.CardId == cardId)
				.Include(co => co.CardCondition)
				.Include(co => co!.CardCondition!.Card)
				.Select(co => CardOwnedMapper.ToDTO(co))
				.ToListAsync();
		}

		public async Task<bool> AddUserCard(User user, CreateCardOwnedDTO cardToAdd)
		{
			string prefix = $"user_{user.Username.ToLower()}";

			// Make sure card with provided id exists
			var card = await _context.Cards
				.Include(c => c.Conditions)
				.FirstOrDefaultAsync(c => c.Id == cardToAdd.CardId);
			if (card == null) return false;

			// Retrieve the specified condition of the card
			Condition condition = (Condition)Enum.Parse(typeof(Condition), cardToAdd.Condition);
			var cardCondition = card.Conditions.FirstOrDefault(c => c.Condition == condition);
			if (cardCondition == null) return false;

			// Make sure the user doesn't already have this card in this condition
			var hasCardCondition = await _context.CardsOwned
				.AnyAsync(c => c.UserId == user.Id && c!.CardCondition!.Id == cardCondition.Id);
			if (hasCardCondition) return false;

			CardOwned cardOwned = CardOwnedMapper.ToModel(cardCondition, cardToAdd.Quantity, user);
			user.CardsOwned.Add(cardOwned);

			// After adding the card, reset cache if it exists
			await _cacheHelper.ClearCacheEntries(prefix);

			return await SaveAsync();
		}

		public async Task<bool> UpdateUserCard(User user, int id, UpdateCardOwnedDTO cardToUpdate)
		{
			string prefix = $"user_{user.Username.ToLower()}";

			// Make sure card with provided id exists
			var cardOwned = _context.CardsOwned.Find(id);
			if (cardOwned == null || cardOwned!.User!.Id != user.Id) return false;
			
			cardOwned.Quantity = cardToUpdate.Quantity;

			// After updating the card in user's collection, reset cache
			await _cacheHelper.ClearCacheEntries(prefix);

			return await SaveAsync();
		}

		public async Task<bool> DeleteUserCard(User user, int id)
		{
			string prefix = $"user_{user.Username.ToLower()}";

			var cardOwned = _context.CardsOwned.Include(co => co.User).FirstOrDefault(co => co.Id == id);
			if (cardOwned == null) return false;
			else if (cardOwned.User != user) return false;

			_context.CardsOwned.Remove(cardOwned);

			// After removing the card, reset cache if it exists
			await _cacheHelper.ClearCacheEntries(prefix);

			return await SaveAsync();
		}

		public async Task<string?> CreatePasswordResetToken(string email)
		{
			var isValidEmail = await _context.Users.Where(u => u.Email == email).AnyAsync();
			if (!isValidEmail) return null;

			var token = ResetTokenHelper.GenerateResetToken();
			var resetToken = new PasswordResetToken 
			{ 
				Token = token,
				Email = email,
				Expiration = DateTime.UtcNow.AddHours(1)
			};

			// Save token to DB
			await _context.PasswordResetTokens.AddAsync(resetToken);
			await _context.SaveChangesAsync();

			return token;
		}

		public async Task<bool> VerifyPasswordResetToken(string token)
		{
			return await _context.PasswordResetTokens.Where(prt => prt.Token == token).AnyAsync();
		}

		public async Task<bool> ResetPassword(string token, string password)
		{
			var resetToken = await _context.PasswordResetTokens.FirstOrDefaultAsync(prt => prt.Token == token);

			if (resetToken == null || resetToken.Expiration < DateTime.UtcNow)
				return false;

			var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == resetToken.Email);
			if (user == null) return false;

			(var hashedPassword, var salt) = PasswordHashHelper.GenerateHashedPasswordAndSalt(password);
			user.Password = hashedPassword;
			user.Salt = Convert.ToBase64String(salt);

			// Delete password reset token from DB
			_context.Remove(resetToken);

			return await SaveAsync();
		}

		public bool Save()
		{
			var saved = _context.SaveChanges();
			return saved > 0;
		}

		public async Task<bool> SaveAsync()
		{
			var saved = await _context.SaveChangesAsync();
			return saved > 0;
		}
	}
}
