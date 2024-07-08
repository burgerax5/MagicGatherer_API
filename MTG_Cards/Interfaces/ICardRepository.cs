using MTG_Cards.DTOs;
using MTG_Cards.Models;

namespace MTG_Cards.Interfaces
{
    public interface ICardRepository
    {
        Task<CardPageDTO> GetCards(int page, string? search, int? editionId, string? sortBy, string? foilFilter);
		Task<CardDTO?> GetCardById(int id);
    }
}
