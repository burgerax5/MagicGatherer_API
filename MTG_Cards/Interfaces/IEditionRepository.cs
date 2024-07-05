using MTG_Cards.DTOs;
using MTG_Cards.Models;

namespace MTG_Cards.Interfaces
{
    public interface IEditionRepository
    {
        Task<List<EditionDropdownDTO>> GetEditionNames();
        Task<EditionDTO?> GetEditionById(int id);
    }
}
