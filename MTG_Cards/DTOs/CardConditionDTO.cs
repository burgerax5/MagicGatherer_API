using MTG_Cards.Models;

namespace MTG_Cards.DTOs
{
	public record struct CardConditionDTO(string Condition, double Price, int Quantity);
}
