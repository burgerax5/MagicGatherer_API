namespace MTG_Cards.DTOs
{
	public record struct CardDetailedDTO (
		CardDTO cardDTO,
		List<CardConditionDTO> cardConditions);
}
