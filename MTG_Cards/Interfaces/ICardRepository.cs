using MTG_Cards.DTOs;
using MTG_Cards.Models;

namespace MTG_Cards.Interfaces
{
    public interface ICardRepository
    {
        Task<ICollection<CardDTO>> GetCards(int page);
        Task<bool> CreateCards(string editionName, ICollection<CardDTO> cards);
        Card GetCardById(int id);
        Task<ICollection<CardDTO>> GetCardsByName(string name);
    }
}
