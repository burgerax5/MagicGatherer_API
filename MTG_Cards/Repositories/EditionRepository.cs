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
    public class EditionRepository : IEditionRepository
    {
        private readonly DataContext _context;
        private readonly IDistributedCache _distributedCache;

        public EditionRepository(DataContext context, IDistributedCache distributedCache)
        {
            _context = context;
            _distributedCache = distributedCache;
        }

        public ICollection<EditionDropdownDTO> GetEditions()
        {
            ICollection<Edition> editions = _context.Editions.ToList();
            ICollection<EditionDropdownDTO> editionDTOs = new List<EditionDropdownDTO>();
            foreach (Edition edition in editions)
            {
                editionDTOs.Add(new EditionDropdownDTO(edition.Name, edition.Code));
            }

			return editionDTOs;
        }

        public async Task<EditionDTO?> GetEditionById(int id)
        {
            string key = $"edition-{id}";
            CancellationToken cancellationToken = default;

            string? cachedEdition = await _distributedCache.GetStringAsync(
                key,
                cancellationToken);
            EditionDTO? editionDTO;

            // Cache Miss
            if (string.IsNullOrEmpty(cachedEdition))
            {
				var edition = await _context.Editions
				        .Include(e => e.Cards)
					        .ThenInclude(c => c.Conditions)
				        .FirstOrDefaultAsync(e => e.Id == id);

                if (edition == null) return null;
                editionDTO = EditionMapper.ToDTO(edition);

				await _distributedCache.SetStringAsync(
                    key, 
                    JsonConvert.SerializeObject(editionDTO),
                    cancellationToken);

				return editionDTO;
			}

            // Cache Hit
            editionDTO = JsonConvert.DeserializeObject<EditionDTO>(cachedEdition);
            return editionDTO;
        }

        public Edition? GetEditionByName(string name)
        {
            return _context.Editions.FirstOrDefault(e => e.Name == name);
        }
    }
}
