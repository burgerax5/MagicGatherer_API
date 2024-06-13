using MTG_Cards.DTOs;
using MTG_Cards.Models;

namespace MTG_Cards.Interfaces
{
    public interface IEditionRepository
    {
        bool EditionExists(int id);
        bool EditionExists(string code);
        ICollection<EditionDropdownDTO> GetEditions();
        Task<Edition?> GetEditionById(int id);
        Edition? GetEditionByName(string name);
        bool CreateEdition(EditionDropdownDTO request);
        bool RemoveEdition(int id);
        bool Save();
    }
}
