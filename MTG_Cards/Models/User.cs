namespace MTG_Cards.Models
{
	public class User
	{
		public int Id { get; set; }
		public required string Username { get; set; }
		public required string Password { get; set; }
		public required string Salt { get; set; }
		public List<CardOwned> CardsOwned { get; set; } = new List<CardOwned>();
	}
}
