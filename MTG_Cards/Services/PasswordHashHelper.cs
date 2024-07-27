using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;

namespace MTG_Cards.Services
{
	public class PasswordHashHelper
	{
		public static (string hashedPassword, byte[] salt) GenerateHashedPasswordAndSalt(string password)
		{
			byte[] salt = RandomNumberGenerator.GetBytes(128 / 8);
			string hashedPassword = Convert.ToBase64String(KeyDerivation.Pbkdf2(
					password: password,
					salt: salt,
					prf: KeyDerivationPrf.HMACSHA256,
					iterationCount: 10000,
					numBytesRequested: 256 / 8
					));
			return (hashedPassword, salt);
		}

		public static string HashPassword(string password, string salt)
		{
			byte[] saltBytes = Convert.FromBase64String(salt);
			string hashedPassword = Convert.ToBase64String(KeyDerivation.Pbkdf2(
					password: password,
					salt: saltBytes,
					prf: KeyDerivationPrf.HMACSHA256,
					iterationCount: 10000,
					numBytesRequested: 256 / 8
					));
			return hashedPassword;
		}
	}
}
