using MTG_Cards.Models;

namespace MTG_Cards.DTOs
{
	public record struct CardDTO(
		int Id,
		string EditionName,
		string EditionCode,
		Rarity Rarity,
		string Name,
		string ImageURL,
		List<CardConditionDTO> CardConditions,
		bool IsFoil,
		double NMPrice);
}
