using MTG_Cards.Models;

namespace MTG_Cards.DTOs
{
	public record struct CardDTO(
		int Id,
		string EditionName,
		int EditionId,
		Rarity Rarity,
		string Name,
		string ImageURL,
		bool IsFoil,
		double NMPrice);
}
