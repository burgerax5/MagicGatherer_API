using System.Security.Cryptography;

namespace MTG_Cards.Services
{
	public class ResetTokenHelper
	{
		public static string GenerateResetToken()
		{
			var rng = RandomNumberGenerator.Create();
			var salt = new byte[32];
			rng.GetBytes(salt);
			return Convert.ToBase64String(salt);
		}
	}
}
