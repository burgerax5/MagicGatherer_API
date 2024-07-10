using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using MTG_Cards.Data;
using MTG_Cards.DTOs;
using MTG_Cards.Interfaces;
using MTG_Cards.Models;
using MTG_Cards.Services;
using MTG_Cards.Services.Mappers;
using Newtonsoft.Json;

namespace MTG_Cards.Repositories
{
    public class EditionRepository : IEditionRepository
    {
        private readonly DataContext _context;
        private readonly IDistributedCache _distributedCache;

        public EditionRepository(DataContext context, IDistributedCache distributedCache)
        {
            _context = context;
            _distributedCache = distributedCache;
        }

        public async Task<List<EditionNameDTO>> GetEditionNames()
        {
            string key = "edition_names";

            var cachedEditions = await Cache.GetCacheEntry<List<EditionNameDTO>?>(_distributedCache, key);

            if (cachedEditions == null) 
            {
				List<Edition> editions = await _context.Editions.ToListAsync();
                List<EditionNameDTO> editionDTOs = editions.Select(edition => EditionMapper.ToGroupedDTO(edition)).ToList();

                await Cache.SetCacheEntry(_distributedCache, key, editionDTOs);

                return editionDTOs;
			}

			return cachedEditions;
        }

        public async Task<List<EditionDropdownDTO>> GetEditionsDropdown()
        {
            string key = "editions_dropdown";
            var cachedEditionDropdown = await Cache.GetCacheEntry<List<EditionDropdownDTO>?>(_distributedCache, key);

            if (cachedEditionDropdown == null)
            {
				List<Edition> editions = await _context.Editions.ToListAsync();
				List<EditionDropdownDTO> editionsDropdown = editions.Select(edition => EditionMapper.ToDropdownDTO(edition)).ToList();

				await Cache.SetCacheEntry(_distributedCache, key, editionsDropdown);

				return editionsDropdown;
			}

            return cachedEditionDropdown;

		}

        public async Task<EditionDTO?> GetEditionById(int id)
        {
            string key = $"edition-{id}";

            var cachedEdition = await Cache.GetCacheEntry<EditionDTO?>(_distributedCache, key);

            if (cachedEdition == null)
            {
				var edition = await _context.Editions
				        .Include(e => e.Cards)
					        .ThenInclude(c => c.Conditions)
				        .FirstOrDefaultAsync(e => e.Id == id);

                if (edition == null) return null;
                var editionDTO = EditionMapper.ToDTO(edition);

                await Cache.SetCacheEntry(_distributedCache, key, editionDTO);

				return editionDTO;
			}

            return cachedEdition;
        }
    }
}
