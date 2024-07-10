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

		public static CardOwnedDTO ToDTO(CardOwned cardOwned)
		{
			Card card = cardOwned.CardCondition!.Card!;
			CardOwnedDTO cardOwnedDTO = new CardOwnedDTO() 
			{ 
				CardId = card.Id,
				CardName = card.Name,
				CardImageURL = card.ImageURL,
				CardPrice = cardOwned.CardCondition.Price,
				EditionName = card.Edition!.Name,
				EditionCode = card.Edition.Code,
				CardOwnedId = cardOwned.Id,
				Condition = cardOwned.CardCondition.Condition.ToString(),
				Quantity = cardOwned.Quantity,
			};

			return cardOwnedDTO;
		}
	}
}
