﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using MTG_Cards.Data;
using MTG_Cards.DTOs;
using MTG_Cards.Interfaces;
using MTG_Cards.Models;
using MTG_Cards.Services.Mappers;
using Newtonsoft.Json;
using MTG_Cards.Services;

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

        public async Task<List<CardDTO>> GetCards(int page, string? search, int? editionId, string? sortBy)
        {
            string key = GenerateCacheKey(page, search, editionId, sortBy);

			var cachedCards = await Cache.GetCacheEntry<List<CardDTO>>(_distributedCache, key);

            if (cachedCards == null)
            {
                var query = ApplyCardFilters(search, editionId, sortBy);

				List<Card> cards = await query
				    .Skip(page * 50)
				    .Take(50)
				    .ToListAsync();

				List<CardDTO> cardsDTO = cards.Select(card => CardMapper.ToDTO(card)).ToList();

				await Cache.SetCacheEntry(_distributedCache, key, cardsDTO);

				return cardsDTO;
			}

            return cachedCards;
        }

        private IQueryable<Card> ApplyCardFilters(string? search, int? editionId, string? sortBy)
        {
            IQueryable<Card> query = _context.Cards
				.Include(c => c.Edition)
				.Include(c => c.Conditions);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(c => c.Name.ToLower().Contains(search.ToLower()));

            if (editionId.HasValue)
                query = query.Where(c => c.EditionId == editionId.Value);

			switch (sortBy)
			{
				case "name_asc":
					query = query.OrderBy(c => c.Name);
					break;
				case "name_desc":
					query = query.OrderByDescending(c => c.Name);
					break;
				case "price_asc":
					query = query.OrderBy(c => c.Conditions
						.Where(cond => cond.Condition == Condition.NM)
						.Select(cond => cond.Price)
						.FirstOrDefault());
					break;
				case "price_desc":
					query = query.OrderByDescending(c => c.Conditions
						.Where(cond => cond.Condition == Condition.NM)
						.Select(cond => cond.Price)
						.FirstOrDefault());
					break;
				case "rarity_asc":
					query = query.OrderBy(c => c.Rarity);
					break;
				case "rarity_desc":
					query = query.OrderByDescending(c => c.Rarity);
					break;
				case "hide_foils":
					query = query.Where(c => !c.IsFoil);
					break;
				default:
					break;
			}

			return query;
        }

		public string GenerateCacheKey(int page, string? search, int? editionId, string? sortBy)
		{
			return $"cards_page_{page}_search_{search ?? "none"}_edition_{editionId?.ToString() ?? "none"}_sort_{sortBy ?? "none"}";
		}

        public async Task<CardDTO?> GetCardById(int id)
        {
            string key = $"cards-{id}";

			var cachedCard = await Cache.GetCacheEntry<CardDTO?>(_distributedCache, key);

            if (cachedCard == null)
            {
				var card = await _context.Cards
				        .Include(c => c.Edition)
				        .Include(c => c.Conditions)
				        .FirstOrDefaultAsync(c => c.Id == id);

                if (card == null) return null;

                var cardDTO = CardMapper.ToDTO(card);
				await Cache.SetCacheEntry(_distributedCache, key, cardDTO);

                return cardDTO;
			}

            return cachedCard;
        }
    }
}
