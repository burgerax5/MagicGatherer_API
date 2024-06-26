using MTG_Cards.Models;

namespace MTG_Cards.DTOs
{
	public record struct CardOwnedDTO(
		int CardId, 
		string CardName, 
		string CardImageURL,
		int CardOwnedId,
		double CardPrice,
		string EditionName,
		string EditionCode,
		string Condition, 
		int Quantity);
}
