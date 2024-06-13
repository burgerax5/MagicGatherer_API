using MTG_Cards.DTOs;
using MTG_Cards.Models;

namespace MTG_Cards.Interfaces
{
    public interface IEditionRepository
    {
        ICollection<EditionDropdownDTO> GetEditions();
        Task<EditionDTO?> GetEditionById(int id);
        Edition? GetEditionByName(string name);
    }
}
