using Microsoft.AspNetCore.Mvc;
using MTG_Cards.DTOs;
using MTG_Cards.Interfaces;
using MTG_Cards.Services;
using System.Security.Cryptography;
using System.Text;

namespace MTG_Cards.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class UserController : ControllerBase
	{
		private readonly IUserRepository _repository;
		public UserController(IUserRepository repository)
		{
			_repository = repository;
		}

		[HttpGet("cards/{username}")]
		public async Task<IActionResult> GetUserCards(string username)
		{
			if (Request.Cookies.TryGetValue("auth", out var authCookie) && VerifyCookie(authCookie) && authCookie.Split('.')[0] == username)
			{
				List<CardOwnedDTO> cardsOwned = await _repository.GetCardsOwned(username);
				return Ok(cardsOwned);
			}

			return StatusCode(403);
		}

		[HttpPost("cards")]
		public async Task<IActionResult> AddCardToUser([FromBody] CreateCardOwnedDTO cardToAdd)
		{
			if (Request.Cookies.TryGetValue("auth", out var authCookie) && VerifyCookie(authCookie))
			{
				var username = authCookie.Split('.')[0];
				var user = _repository.GetUserByUsername(username);
				var isSuccess = await _repository.AddUserCard(user, cardToAdd);
				if (isSuccess) return Ok("Successfully added cards to collection");
				return BadRequest("Something went wrong while trying to add card to collection");
			}

			return StatusCode(403);
		}

		[HttpPut("cards/{id}")]
		public async Task<IActionResult> UpdateUserCard(int id, [FromBody] UpdateCardOwnedDTO updatedCardDetails)
		{
			if (Request.Cookies.TryGetValue("auth", out var authCookie) && VerifyCookie(authCookie))
			{
				var username = authCookie.Split('.')[0];
				var user = _repository.GetUserByUsername(username);
				var isSuccess = await _repository.UpdateUserCard(user, id, updatedCardDetails);
				if (isSuccess) return Ok("Successfully updated card in collection");
				return BadRequest("Something went wrong while trying to update card in collection");
			}

			return StatusCode(403);
		}

		[HttpDelete("cards/{id}")]
		public async Task<IActionResult> DeleteUserCard(int id)
		{
			if (Request.Cookies.TryGetValue("auth", out var authCookie) && VerifyCookie(authCookie))
			{
				var username = authCookie.Split('.')[0];
				var user = _repository.GetUserByUsername(username);
				var isSuccess = await _repository.DeleteUserCard(user, id);
				if (isSuccess) return Ok("Successfully removed card from collection");
				return BadRequest("Card is not in your collection");
			}

			return StatusCode(403);
		}

		[HttpPost("login")]
		public IActionResult LoginUser(UserLoginDTO userLoginDTO)
		{
			if (!_repository.UserExists(userLoginDTO.Username))
				return NotFound("User not found");

			bool successfulLogin = _repository.LoginUser(userLoginDTO);
			if (!successfulLogin)
				return BadRequest("Invalid user credentials");

			var cookieOptions = new CookieOptions
			{
				Expires = DateTime.Now.AddDays(1),
				Secure = true,
				HttpOnly = true,
				SameSite = SameSiteMode.Strict
			};

			var signature = GenerateSignature(userLoginDTO.Username);

			Response.Cookies.Append("auth", $"{userLoginDTO.Username}.{signature}", cookieOptions);
			return Ok("Successfully logged in");
		}

		[HttpPost("register")]
		public IActionResult RegisterUser([FromBody] UserLoginDTO userDTO)
		{
			if (_repository.UserExists(userDTO.Username))
				return BadRequest("Username is already taken");

			bool successfulRegister = _repository.RegisterUser(userDTO);
			if (!successfulRegister)
				return BadRequest("Something went wrong while registering user");

			return Ok("Successfully registered: " + userDTO.Username);
		}

		[HttpPost("logout")]
		public IActionResult LogoutUser()
		{
			Response.Cookies.Delete("auth");
			return Ok("Logged out successfully");
		}

		private bool VerifyCookie(string signedCookie)
		{
			var parts = signedCookie.Split('.');
			if (parts.Length != 2) return false;

			var data = parts[0];
			var receivedSignature = parts[1];

			var expectedSignature = GenerateSignature(data);
			return receivedSignature == expectedSignature;
		}

		private string GenerateSignature(string username)
		{
			DotNetEnv.Env.Load();
			var secretKey = Environment.GetEnvironmentVariable("SECRET_KEY");

			using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey)))
			{
				var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(username));
				return Convert.ToBase64String(hash);
			}
		}
	}
}
