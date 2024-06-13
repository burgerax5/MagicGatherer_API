using MTG_Cards.DTOs;
using MTG_Cards.Models;

namespace MTG_Cards.Interfaces
{
	public interface IUserRepository
	{
		bool UserExists(string username);
		User? GetUserById(int id);
		User? GetUserByUsername(string username);
		List<CardOwnedDTO> GetCardsOwned(string username);
		bool LoginUser(UserLoginDTO user);
		bool RegisterUser(UserLoginDTO user);
		bool AddUserCard(User user, CreateCardOwnedDTO card);
		bool UpdateUserCard(User user, int id, UpdateCardOwnedDTO updatedCardDetails);
		bool DeleteUserCard(User user, int id);
		bool Save();
	}
}
