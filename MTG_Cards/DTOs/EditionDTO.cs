namespace MTG_Cards.DTOs
{
	public record struct EditionDTO(
		int Id,
		string Name,
		List<CardDTO> Cards
		);
}
