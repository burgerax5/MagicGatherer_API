namespace MTG_Cards.Models
{
	public class PasswordResetToken
	{
		public int Id { get; set; }
		public required string Token { get; set; }
		public required string Email { get; set; }
		public DateTime Expiration {  get; set; }
	}
}
