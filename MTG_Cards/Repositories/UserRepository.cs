using MTG_Cards.Data;
using MTG_Cards.Models;
using MTG_Cards.DTOs;
using MTG_Cards.Services.Mappers;
using MTG_Cards.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Claims;
using Microsoft.Net.Http.Headers;
using Azure;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Threading;

namespace MTG_Cards.Repositories
{
	public class UserRepository : IUserRepository
	{
		private readonly DataContext _context;
		private readonly IDistributedCache _distributedCache;

		public UserRepository(DataContext context, IDistributedCache distributedCache)
		{
			_context = context;
			_distributedCache = distributedCache;
		}

		public bool UserExists(string username)
		{
			return _context.Users.Any(u => u.Username == username);
		}

		public User? GetUserById(int id)
		{
			return _context.Users
				.Include(u => u.CardsOwned)
					.ThenInclude(co => co.CardCondition)
					.ThenInclude(cd => cd.Card)
					.ThenInclude(c => c.Edition)
				.FirstOrDefault(u => u.Id == id);
		}

		public User? GetUserByUsername(string username) 
		{
			return _context.Users
				.Include(u => u.CardsOwned)
					.ThenInclude(co => co.CardCondition)
					.ThenInclude(cd => cd.Card)
					.ThenInclude(c => c.Edition)
				.FirstOrDefault(u => u.Username == username);
		}

		public async Task<List<CardOwnedDTO>> GetCardsOwned(string username)
		{
			string key = $"user-{username.ToLower()}";
			CancellationToken cancellationToken = default;

			string? cachedCards = await _distributedCache.GetStringAsync(
				key,
				cancellationToken);

			if (string.IsNullOrEmpty(cachedCards))
			{
				var user = _context.Users
								.Include(u => u.CardsOwned)
									.ThenInclude(co => co.CardCondition)
									.ThenInclude(cd => cd.Card)
									.ThenInclude(c => c.Edition)
								.FirstOrDefault(u => u.Username == username);
				List<CardOwnedDTO> cardsOwnedDTO = new List<CardOwnedDTO>();

				if (user == null) return cardsOwnedDTO;

				foreach (CardOwned cardOwned in user.CardsOwned)
					cardsOwnedDTO.Add(CardOwnedMapper.ToDTO(cardOwned));

				await _distributedCache.SetStringAsync(
					key,
					JsonConvert.SerializeObject(cardsOwnedDTO),
					cancellationToken);

				return cardsOwnedDTO;
			}

			var deserializedCards = JsonConvert.DeserializeObject<List<CardOwnedDTO>>(cachedCards);
			if (deserializedCards == null) deserializedCards = [];
			return deserializedCards;
		}

		public bool LoginUser(UserLoginDTO userDTO)
		{
			User? user = _context.Users.FirstOrDefault(u => u.Username == userDTO.Username);
			if (user == null) return false;

			string hashedPassword = HashPassword(userDTO.Password, user.Salt);
			if (hashedPassword != user.Password) return false;

			return true;
		}

		public bool RegisterUser(UserLoginDTO userDTO)
		{
			var (hashedPassword, salt) = GenerateHashedPasswordAndSalt(userDTO.Password);
			userDTO.Password = hashedPassword;
			_context.Users.Add(UserMapper.ToModel(userDTO, salt));
			return Save();
		}

		public async Task<bool> AddUserCard(User user, CreateCardOwnedDTO cardToAdd)
		{
			string key = $"user-{user.Username.ToLower()}";
			CancellationToken cancellationToken = default;

			string? cachedCards = await _distributedCache.GetStringAsync(
				key,
				cancellationToken);

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
				.AnyAsync(c => c.UserId == user.Id && c.CardCondition.Condition == condition);
			if (hasCardCondition) return false;

			CardOwned cardOwned = CardOwnedMapper.ToModel(cardCondition, cardToAdd.Quantity, user);
			user.CardsOwned.Add(cardOwned);

			// After adding the card, reset cache if it exists
			if (!string.IsNullOrEmpty(cachedCards))
			{
				await _distributedCache.RemoveAsync(key, cancellationToken);
			}

			return await SaveAsync();
		}

		public async Task<bool> UpdateUserCard(User user, int id, UpdateCardOwnedDTO cardToUpdate)
		{
			string key = $"user-{user.Username.ToLower()}";
			CancellationToken cancellationToken = default;

			string? cachedCards = await _distributedCache.GetStringAsync(
				key,
				cancellationToken);

			// Make sure card with provided id exists
			var cardOwned = _context.CardsOwned.Find(id);
			if (cardOwned == null || cardOwned.User != user) return false;
			
			cardOwned.Quantity = cardToUpdate.Quantity;

			// After updating the card in user's collection, reset cache
			if (!string.IsNullOrEmpty(cachedCards))
			{
				await _distributedCache.RemoveAsync(key, cancellationToken);
			}

			return await SaveAsync();
		}

		public async Task<bool> DeleteUserCard(User user, int id)
		{
			string key = $"user-{user.Username.ToLower()}";
			CancellationToken cancellationToken = default;

			string? cachedCards = await _distributedCache.GetStringAsync(
				key,
				cancellationToken);

			var cardOwned = _context.CardsOwned.Include(co => co.User).FirstOrDefault(co => co.Id == id);
			if ( cardOwned == null) return false;
			else if (cardOwned.User != user) return false;

			_context.CardsOwned.Remove(cardOwned);

			// After removing the card, reset cache if it exists
			if (!string.IsNullOrEmpty(cachedCards))
			{
				await _distributedCache.RemoveAsync(key, cancellationToken);
			}

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

		private (string hashedPassword, byte[] salt) GenerateHashedPasswordAndSalt(string password)
		{
			byte[] salt = RandomNumberGenerator.GetBytes(128 / 8);
			string hashedPassword = Convert.ToBase64String(KeyDerivation.Pbkdf2(
					password: password,
					salt: salt,
					prf: KeyDerivationPrf.HMACSHA256,
					iterationCount: 10000,
					numBytesRequested: 256 / 8
					));
			return (hashedPassword, salt);
		}

		private string HashPassword(string password, string salt)
		{
			byte[] saltBytes = Convert.FromBase64String(salt);
			string hashedPassword = Convert.ToBase64String(KeyDerivation.Pbkdf2(
					password: password,
					salt: saltBytes,
					prf: KeyDerivationPrf.HMACSHA256,
					iterationCount: 10000,
					numBytesRequested: 256 / 8
					));
			return hashedPassword;
		}
	}
}
