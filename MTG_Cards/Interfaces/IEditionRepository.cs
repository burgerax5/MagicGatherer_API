using MTG_Cards.DTOs;

namespace MTG_Cards.Interfaces
{
    public interface IEditionRepository
    {
        Task<List<EditionNameDTO>> GetEditionNames();
        Task<List<GroupedEditionNames>> GetEditionNamesGrouped();
        Task<EditionDTO?> GetEditionById(int id);
        Task<List<EditionDropdownDTO>> GetEditionsDropdown();
    }
}
