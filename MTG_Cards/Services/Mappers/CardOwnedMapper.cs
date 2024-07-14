using MTG_Cards.DTOs;
using MTG_Cards.Models;

namespace MTG_Cards.Services.Mappers
{
	public class CardOwnedMapper
	{
		public static CardOwned ToModel(CardCondition cardCondition, int quantity, User user)
		{
			CardOwned cardOwned = new CardOwned()
			{
				CardCondition = cardCondition,
				Quantity = quantity,
				User = user,
			};

			return cardOwned;
		}
	}
}
