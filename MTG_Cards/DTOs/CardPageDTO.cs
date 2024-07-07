namespace MTG_Cards.DTOs
{
	public record struct CardPageDTO(
		int curr_page,
		int total_pages,
		int results,
		List<CardDTO> CardDTOs);
}
