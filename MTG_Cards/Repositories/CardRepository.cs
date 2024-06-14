using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using MTG_Cards.Data;
using MTG_Cards.DTOs;
using MTG_Cards.Interfaces;
using MTG_Cards.Models;
using MTG_Cards.Services.Mappers;
using Newtonsoft.Json;

namespace MTG_Cards.Repositories
{
    public class CardRepository : ICardRepository
    {
        private readonly DataContext _context;
        private readonly IDistributedCache _distributedCache;

        public CardRepository(DataContext context, IDistributedCache distributedCache)
        {
            _context = context;
            _distributedCache = distributedCache;
        }

        public bool CardExists(int id)
        {
            return _context.Cards.Any(c => c.Id == id);
        }

        public async Task<ICollection<CardDTO>> GetCards(int page)
        {
            string key = $"all_cards_page_{page}";
			CancellationToken cancellationToken = default;

			string? cachedCards = await _distributedCache.GetStringAsync(
				key,
				cancellationToken);

            if (string.IsNullOrEmpty(cachedCards))
            {
				List<Card> cards = await _context.Cards
				.Skip(page * 50)
				.Take(50)
				.Include(c => c.Edition)
				.Include(c => c.Conditions)
				.ToListAsync();

				List<CardDTO> cardsDTO = new List<CardDTO>();
				foreach (Card card in cards)
				{
					CardDTO cardDTO = CardMapper.ToDTO(card);
					cardsDTO.Add(cardDTO);
				}

                await _distributedCache.SetStringAsync(
                    key,
                    JsonConvert.SerializeObject(cardsDTO),
                    cancellationToken);

                return cardsDTO;
			}

            return JsonConvert.DeserializeObject<ICollection<CardDTO>>(cachedCards);
        }

        public Card GetCardById(int id)
        {
            var card = _context.Cards
                .Include(c => c.Edition)
                .Include(c => c.Conditions)
                .FirstOrDefault(c => c.Id == id);

            return card;
        }

        public async Task<ICollection<CardDTO>> GetCardsByName(string name)
        {
            string normalizedName = name.ToLower().Trim();
            string key = $"cards-search-{normalizedName}";
			CancellationToken cancellationToken = default;

			string? cachedCards = await _distributedCache.GetStringAsync(
				key,
				cancellationToken);

            if (string.IsNullOrEmpty(cachedCards))
            {
                var cards = await _context.Cards
	                .Include(c => c.Edition)
	                .Include(c => c.Conditions)
	                .Where(cards => cards.Name.Contains(name))
                    .ToListAsync();

                ICollection<CardDTO> cardDTOs = new List<CardDTO>();
                foreach (var card in cards)
                {
                    cardDTOs.Add(CardMapper.ToDTO(card));
                }

                await _distributedCache.SetStringAsync(
                    key,
                    JsonConvert.SerializeObject(cardDTOs),
                    cancellationToken);

                return cardDTOs;
			}

            var deserializedCards = JsonConvert.DeserializeObject<ICollection<CardDTO>>(cachedCards);
            return deserializedCards;
        }

        public async Task<bool> CreateCards(string editionName, ICollection<CardDTO> cards)
        {
			//Check if the edition already exists in the database
			var existingEdition = await _context.Editions.FirstOrDefaultAsync(e => e.Name == editionName);

            if (existingEdition == null)
                return false;

			foreach (CardDTO card in cards)
            {
                await _context.Cards.AddAsync(CardMapper.ToModel(existingEdition, card));
            }

            return await SaveAsync();
        }

        public bool CreateCard(string editionName, Card card)
        {
            //Check if the edition already exists in the database
            var existingEdition = _context.Editions.FirstOrDefault(e => e.Name == editionName);

            if (existingEdition == null)
                return false;

            card.Edition = existingEdition;
            _context.Cards.Add(card);
            return Save();
        }

        public bool RemoveCard(Card card)
        {
            _context.Cards.Remove(card);
            return Save();
        }

        public bool Save()
        {
            var saved = _context.SaveChanges();
            return saved > 0 ? true : false;
        }

        public async Task<bool> SaveAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        //public void UpdateCard(intCard card)
        //{

        //}
    }
}
