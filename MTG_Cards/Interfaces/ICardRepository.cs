using MTG_Cards.DTOs;
using MTG_Cards.Models;

namespace MTG_Cards.Interfaces
{
    public interface ICardRepository
    {
        Task<ICollection<CardDTO>> GetCards(int page);
		Task<CardDTO?> GetCardById(int id);
        Task<ICollection<CardDTO>> GetCardsByName(string name);
    }
}
