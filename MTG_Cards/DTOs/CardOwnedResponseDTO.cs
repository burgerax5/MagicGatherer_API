namespace MTG_Cards.DTOs
{
	public record struct CardOwnedResponseDTO
	(
		int TotalCardsOwned,
		double EstimatedValue,
		List<CardOwnedDTO> CardsOwned
	);
}
