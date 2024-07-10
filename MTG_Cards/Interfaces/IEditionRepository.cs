using MTG_Cards.DTOs;
using MTG_Cards.Models;

namespace MTG_Cards.Interfaces
{
    public interface IEditionRepository
    {
        Task<List<EditionNameDTO>> GetEditionNames();
        Task<EditionDTO?> GetEditionById(int id);
        Task<List<EditionDropdownDTO>> GetEditionsDropdown();
    }
}
