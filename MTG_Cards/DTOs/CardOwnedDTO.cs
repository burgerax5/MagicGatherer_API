using MTG_Cards.Models;

namespace MTG_Cards.DTOs
{
	public record struct CardOwnedDTO(
		int Id,
		int CardId,
		string Condition,
		int Quantity);
}
