using MTG_Cards.DTOs;
using MTG_Cards.Models;

namespace MTG_Cards.Services.Mappers
{
	public class CardConditionMapper
	{
		public static CardCondition ToModel(Card card, CardConditionDTO cardConditionDTO)
		{
			Enum.TryParse(cardConditionDTO.Condition, out Condition condition);
			CardCondition cardCondition = new CardCondition()
			{
				Card = card,
				Condition = condition,
				Price = cardConditionDTO.Price,
				Quantity = cardConditionDTO.Quantity,
			};

			return cardCondition;
		}

		public static CardConditionDTO ToDTO(CardCondition cardCondition)
		{
			CardConditionDTO cardConditionDTO = new CardConditionDTO()
			{
				Condition = cardCondition.Condition.ToString(),
				Price = cardCondition.Price,
				Quantity = cardCondition.Quantity,
			};

			return cardConditionDTO;
		}
	}
}
