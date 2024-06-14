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

        public async Task<CardDTO?> GetCardById(int id)
        {
            string key = $"cards-{id}";
			CancellationToken cancellationToken = default;

			string? cachedCard = await _distributedCache.GetStringAsync(
				key,
				cancellationToken);

            if (string.IsNullOrEmpty(cachedCard))
            {
				var card = await _context.Cards
				        .Include(c => c.Edition)
				        .Include(c => c.Conditions)
				        .FirstOrDefaultAsync(c => c.Id == id);

                if (card == null) return null;

                var cardDTO = CardMapper.ToDTO(card);
                await _distributedCache.SetStringAsync(
                    key,
                    JsonConvert.SerializeObject(cardDTO),
                    cancellationToken);

                return cardDTO;
			}

			var deserializedCard = JsonConvert.DeserializeObject<CardDTO>(cachedCard);
            return deserializedCard;
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
    }
}
