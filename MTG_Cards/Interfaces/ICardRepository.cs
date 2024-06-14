using MTG_Cards.DTOs;
using MTG_Cards.Models;

namespace MTG_Cards.Interfaces
{
    public interface ICardRepository
    {
        bool CardExists(int id);
        Task<ICollection<CardDTO>> GetCards(int page);
        Task<bool> CreateCards(string editionName, ICollection<CardDTO> cards);
        Card GetCardById(int id);
        Task<ICollection<CardDTO>> GetCardsByName(string name);
        bool CreateCard(string editionName, Card card);
        bool RemoveCard(Card card);
        bool Save();
        Task<bool> SaveAsync();
    }
}
