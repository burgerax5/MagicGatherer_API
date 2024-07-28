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
			return Base64URLEncode(salt);
		}

		private static string Base64URLEncode(byte[] input)
		{
			var base64 = Convert.ToBase64String(input);

			// Convert to URL-safe Base64
			return base64.Replace('+', '-')
				.Replace('/', '_')
				.Replace("=", string.Empty);
		}
	}
}
