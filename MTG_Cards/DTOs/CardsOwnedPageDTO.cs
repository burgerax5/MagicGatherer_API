namespace MTG_Cards.DTOs
{
	public record struct CardsOwnedPageDTO
	(
		int TotalCardsOwned,
		double EstimatedValue,
		CardPageDTO CardPageDTO
	);
}
