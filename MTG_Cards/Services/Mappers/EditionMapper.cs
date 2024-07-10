using MTG_Cards.DTOs;
using MTG_Cards.Models;

namespace MTG_Cards.Services.Mappers
{
	public class EditionMapper
	{
		public static EditionDTO ToDTO(Edition edition)
		{
			List<CardDTO> cards = new List<CardDTO>();
			foreach(Card card in edition.Cards)
			{
				cards.Add(CardMapper.ToDTO(card));
			}

			EditionDTO editionDTO = new EditionDTO 
			{ 
				Id = edition.Id,
				Name = edition.Name,
				Cards = cards
			};

			return editionDTO;
		}

		public static EditionNameDTO ToGroupedDTO(Edition edition)
		{
			return new EditionNameDTO
			{
				EditionId = edition.Id,
				Code = edition.Code,
				Name = edition.Name,
			};
		}

		public static EditionDropdownDTO ToDropdownDTO(Edition edition)
		{
			return new EditionDropdownDTO
			{
				Name = edition.Name,
				Value = edition.Id
			};
		}
	}
}
