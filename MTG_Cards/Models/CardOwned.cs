namespace MTG_Cards.Models
{
	public class CardOwned
	{
		public int Id { get; set; }
		public int CardConditionId { get; set; }
		public required CardCondition CardCondition { get; set; }
		public int Quantity { get; set; }
		public int UserId { get; set; }
		public required User User { get; set; }
	}
}
