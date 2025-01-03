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

        public async Task<CardPageDTO> GetCards(int page, string? search, int? editionId, string? sortBy, string? foilFilter)
        {
            string key = GenerateCacheKey(page, search, editionId, sortBy, foilFilter);

			var cachedCards = await CacheHelper.GetCacheEntry<CardPageDTO?>(_distributedCache, key);

            if (cachedCards == null)
            {
				try
				{
					var query = ApplyCardFilters(search, editionId, sortBy, foilFilter);
					var numResults = await query.CountAsync();

					List<Card> cards = await query
						.AsNoTracking()
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
					Console.WriteLine($"Error in GetCards: {ex.Message}");
					throw; // Re-throw the exception after logging
				}
			}

            return cachedCards.Value;
        }

        private IQueryable<Card> ApplyCardFilters(string? search, int? editionId, string? sortBy, string? foilFilter)
        {
			IQueryable<Card> query = _context.Cards
				.AsNoTracking()
				.Include(c => c.Edition);

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

		public string GenerateCacheKey(int page, string? search, int? editionId, string? sortBy, string? foilFilter)
		{
			return $"cards_page_{page}_search_{search ?? "none"}_edition_{editionId?.ToString() ?? "none"}_sort_{sortBy ?? "none"}_foilFilter_{foilFilter ?? "none"}";
		}

        public async Task<CardDetailedDTO?> GetCardById(int id)
        {
            string key = $"cards-{id}";

			var cachedCard = await CacheHelper.GetCacheEntry<CardDetailedDTO?>(_distributedCache, key);

            if (cachedCard == null)
            {
				var card = await _context.Cards
				        .Include(c => c.Edition)
				        .Include(c => c.Conditions)
				        .FirstOrDefaultAsync(c => c.Id == id);

                if (card == null) return null;

                var cardDTO = CardMapper.ToDetailedDTO(card);
				await CacheHelper.SetCacheEntry(_distributedCache, key, cardDTO);

                return cardDTO;
			}

            return cachedCard;
        }
    }
}
