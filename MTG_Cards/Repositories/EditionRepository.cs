using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using MTG_Cards.Data;
using MTG_Cards.DTOs;
using MTG_Cards.Interfaces;
using MTG_Cards.Models;

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

        public async Task<Edition?> GetEditionById(int id)
        {
            var edition = await _context.Editions
                .Include(e => e.Cards)
                    .ThenInclude(c => c.Conditions)
                .FirstOrDefaultAsync(e => e.Id == id);

            return edition;
        }

        public Edition? GetEditionByName(string name)
        {
            return _context.Editions.FirstOrDefault(e => e.Name == name);
        }

   //     public bool CreateEdition(EditionDropdownDTO request)
   //     {
   //         var edition = new Edition 
   //         { 
   //             Name = request.Name,
   //             Code = request.Code,
			//	Cards = new List<Card>()
			//};

   //         _context.Editions.Add(edition);
   //         return Save();
   //     }

   //     public bool RemoveEdition(int id)
   //     {
   //         var edition = _context.Editions.FirstOrDefault(e => e.Id==id);
   //         if (edition == null)
   //             return false;
            
   //         _context.Editions.Remove(edition);
   //         return Save();
   //     }

        public bool Save()
        {
            var saved = _context.SaveChanges();
            return saved > 0 ? true : false;
        }
    }
}
