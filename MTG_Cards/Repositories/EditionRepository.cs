using Microsoft.EntityFrameworkCore;
using MTG_Cards.Data;
using MTG_Cards.DTOs;
using MTG_Cards.Interfaces;
using MTG_Cards.Models;

namespace MTG_Cards.Repositories
{
    public class EditionRepository : IEditionRepository
    {
        private readonly DataContext _context;

        public EditionRepository(DataContext context)
        {
            _context = context;
        }

        public Boolean EditionExists(int id)
        {
            return _context.Editions.Any(e => e.Id == id);
        }

        public Boolean EditionExists(string code)
        {
            return _context.Editions.Any(e => e.Code == code);
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

        public Edition GetEditionByName(string name)
        {
            return _context.Editions.FirstOrDefault(e => e.Name == name);
        }

        public bool CreateEdition(EditionDropdownDTO request)
        {
            var edition = new Edition 
            { 
                Name = request.Name,
                Code = request.Code,
				Cards = new List<Card>()
			};

            _context.Editions.Add(edition);
            return Save();
        }

        public bool RemoveEdition(int id)
        {
            var edition = _context.Editions.FirstOrDefault(e => e.Id==id);
            if (edition == null)
                return false;
            
            _context.Editions.Remove(edition);
            return Save();
        }

        public bool Save()
        {
            var saved = _context.SaveChanges();
            return saved > 0 ? true : false;
        }
    }
}
