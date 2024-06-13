using MTG_Cards.Models;

namespace MTG_Cards.DTOs
{
	public record struct CardOwnedDTO(
		int CardId, 
		string CardName, 
		string EditionName,
		int CardOwnedId, 
		string Condition, 
		int Quantity);
}
