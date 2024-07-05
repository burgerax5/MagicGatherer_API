using MTG_Cards.DTOs;
using MTG_Cards.Models;

namespace MTG_Cards.Interfaces
{
    public interface ICardRepository
    {
        Task<List<CardDTO>> GetCards(int page, string? search, int? editionId, string? sortBy);
		Task<CardDTO?> GetCardById(int id);
    }
}
