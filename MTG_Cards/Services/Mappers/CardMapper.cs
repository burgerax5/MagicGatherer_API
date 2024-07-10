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
				IsFoil = cardDTO.IsFoil
			};

			return card;
		}

		public static CardDTO ToDTO(Card card)
		{
			CardDTO cardDTO = new CardDTO()
			{
				Id = card.Id,
				EditionName = card!.Edition!.Name,
				EditionCode = card.Edition.Code,
				Name = card.Name,
				ImageURL = card.ImageURL,
				IsFoil = card.IsFoil,
				NMPrice = card.NMPrice,
				Rarity = card.Rarity
			};

			return cardDTO;
		}

		public static CardDetailedDTO ToDetailedDTO(Card card)
		{
			CardDTO cardDTO = ToDTO(card);
			//List<CardDTO> cardsDTO = cards.Select(card => CardMapper.ToDTO(card)).ToList();
			List< CardConditionDTO> cardConditionDTO = card.Conditions.Select(condition => CardConditionMapper.ToDTO(condition)).ToList();

			CardDetailedDTO cardDetailedDTO = new CardDetailedDTO(cardDTO, cardConditionDTO);
			return cardDetailedDTO;
		}
	}
}
