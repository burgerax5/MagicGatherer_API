namespace MTG_Cards.DTOs
{
	public record struct CreateCardOwnedDTO(
		int CardId,
		string Condition,
		int Quantity);
}
