using MTG_Cards.DTOs;
using MTG_Cards.Models;

namespace MTG_Cards.Services.Mappers
{
	public class UserMapper
	{
		public static User ToModel(UserLoginDTO userDTO, byte[] salt)
		{
			return new User
			{
				Email = userDTO.Email,
				Username = userDTO.Username,
				Password = userDTO.Password,
				CardsOwned = new List<CardOwned>(),
				Salt = Convert.ToBase64String(salt)
			};
		}
	}
}
