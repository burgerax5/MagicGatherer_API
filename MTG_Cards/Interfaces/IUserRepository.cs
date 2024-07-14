using MTG_Cards.DTOs;
using MTG_Cards.Models;

namespace MTG_Cards.Interfaces
{
	public interface IUserRepository
	{
		bool UserExists(string username);
		User? GetUserById(int id);
		User? GetUserByUsername(string username);
		Task<CardPageDTO> GetCardsOwned(string username, int page, string? search, int? editionId, string? sortBy, string? foilFilter);
		Task<(int totalCards, double totalValue)> GetTotalCardsAndValue(string username);
		bool LoginUser(UserLoginDTO user);
		bool RegisterUser(UserLoginDTO user);
		Task<List<CreateCardOwnedDTO>> GetCardConditions(string username, int cardId);
		Task<bool> AddUserCard(User user, CreateCardOwnedDTO card);
		Task<bool> UpdateUserCard(User user, int id, UpdateCardOwnedDTO updatedCardDetails);
		Task<bool> DeleteUserCard(User user, int id);
		bool Save();
		Task<bool> SaveAsync();
	}
}
