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
		bool LoginUser(UserLoginDTO user);
		bool RegisterUser(UserLoginDTO user);
		Task<bool> AddUserCard(User user, CreateCardOwnedDTO card);
		Task<bool> UpdateUserCard(User user, int id, UpdateCardOwnedDTO updatedCardDetails);
		Task<bool> DeleteUserCard(User user, int id);
		bool Save();
		Task<bool> SaveAsync();
	}
}
