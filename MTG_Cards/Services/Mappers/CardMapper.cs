using MTG_Cards.DTOs;
using MTG_Cards.Models;

namespace MTG_Cards.Services.Mappers
{
	public class CardMapper
	{
		public static Card ToModel(Edition edition, CardDTO cardDTO)
		{
			Card card = new Card()
			{
				Edition = edition,
				Name = cardDTO.Name,
				ImageURL = cardDTO.ImageURL,
				Conditions = new List<CardCondition>(),
				IsFoil = cardDTO.IsFoil
			};

			foreach (CardConditionDTO cardConditionDTO in cardDTO.CardConditions)
			{
				card.Conditions.Add(CardConditionMapper.ToModel(card, cardConditionDTO));
			}

			return card;
		}

		public static CardDTO ToDTO(Card card)
		{
			CardDTO cardDTO = new CardDTO()
			{
				Id = card.Id,
				EditionName = card.Edition.Name,
				EditionCode = card.Edition.Code,
				Name = card.Name,
				ImageURL = card.ImageURL,
				CardConditions = card.Conditions.Select(cond => new CardConditionDTO(
					cond.Condition.ToString(),
					cond.Price,
					cond.Quantity)).ToList(),
				IsFoil = card.IsFoil,
				NMPrice = card.NMPrice,
			};

			return cardDTO;
		}
	}
}
