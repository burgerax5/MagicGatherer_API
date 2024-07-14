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

		public static CreateCardOwnedDTO ToDTO(CardOwned cardOwned)
		{
			var cardCondition = cardOwned.CardCondition;
			CreateCardOwnedDTO cardOwnedDTO = new CreateCardOwnedDTO()
			{
				CardId = cardCondition!.CardId,
				Condition = cardCondition.Condition.ToString(),
				Quantity = cardOwned.Quantity,
			};

			return cardOwnedDTO;
		}
	}
}
