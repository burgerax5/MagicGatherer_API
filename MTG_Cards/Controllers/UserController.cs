using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MTG_Cards.DTOs;
using MTG_Cards.Interfaces;
using MTG_Cards.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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
		[Authorize]
		public async Task<IActionResult> GetUserCards(string username)
		{
			var user = User?.Identity?.Name;
			if (user != username)
				return StatusCode(403);

			List<CardOwnedDTO> cardsOwned = await _repository.GetCardsOwned(username);
			var totalCards = cardsOwned.Count();
			var totalPrice = 0.0;
			foreach (var card in cardsOwned)
			{
				totalPrice += card.Quantity * card.CardPrice;
			}

			CardOwnedResponseDTO responseDTO = new CardOwnedResponseDTO(
				totalCards,
				double.Round(totalPrice, 2, MidpointRounding.AwayFromZero),
				cardsOwned);

			return Ok(responseDTO);
		}

		[HttpPost("cards")]
		[Authorize]
		public async Task<IActionResult> AddCardToUser([FromBody] CreateCardOwnedDTO cardToAdd)
		{
			var username = User?.Identity?.Name;
			if (string.IsNullOrEmpty(username))
				return BadRequest("Username cannot be null");

			var user = _repository.GetUserByUsername(username);
			if (user == null) return BadRequest("Invalid user");

			var isSuccess = await _repository.AddUserCard(user, cardToAdd);
			if (isSuccess) return Ok("Successfully added cards to collection");
			return BadRequest("Something went wrong while trying to add card to collection");
		}

		[HttpPut("cards/{id}")]
		[Authorize]
		public async Task<IActionResult> UpdateUserCard(int id, [FromBody] UpdateCardOwnedDTO updatedCardDetails)
		{
			var username = User?.Identity?.Name;
			if (string.IsNullOrEmpty(username))
				return BadRequest("Username cannot be null");

			var user = _repository.GetUserByUsername(username);
			if (user == null) return BadRequest("Invalid user");

			var isSuccess = await _repository.UpdateUserCard(user, id, updatedCardDetails);
			if (isSuccess) return Ok("Successfully updated card in collection");
			return BadRequest("Something went wrong while trying to update card in collection");
		}

		[HttpDelete("cards/{id}")]
		[Authorize]
		public async Task<IActionResult> DeleteUserCard(int id)
		{
			var username = User?.Identity?.Name;
			if (string.IsNullOrEmpty(username))
				return BadRequest("Username cannot be null");

			var user = _repository.GetUserByUsername(username);
			if (user == null) return BadRequest("Invalid user");

			var isSuccess = await _repository.DeleteUserCard(user, id);
			if (isSuccess) return Ok("Successfully removed card from collection");
			return BadRequest("Card is not in your collection");
		}

		[HttpPost("login")]
		public IActionResult LoginUser(UserLoginDTO userLoginDTO)
		{
			if (!_repository.UserExists(userLoginDTO.Username))
				return NotFound("User not found");

			bool successfulLogin = _repository.LoginUser(userLoginDTO);
			if (!successfulLogin)
				return BadRequest("Invalid user credentials");

			var token = GenerateJwtToken(userLoginDTO.Username);
			return Ok(new { token });
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

		private string GenerateJwtToken(string username)
		{
			try
			{
				DotNetEnv.Env.Load();
				var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
				if (secretKey == null) throw new Exception("Missing JWT_SECRET_KEY");

				var keyEncoded = Encoding.ASCII.GetBytes(secretKey);
				var tokenDescriptor = new SecurityTokenDescriptor
				{
					Subject = new ClaimsIdentity(new[]
					{
					new Claim(ClaimTypes.Name, username)
				}),
					Expires = DateTime.UtcNow.AddDays(1),
					SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyEncoded), SecurityAlgorithms.HmacSha256Signature)
				};

				var tokenHandler = new JwtSecurityTokenHandler();
				var token = tokenHandler.CreateToken(tokenDescriptor);
				return tokenHandler.WriteToken(token);
			} catch (Exception ex)
			{
				Console.WriteLine("Exception: " + ex.ToString());
				return "";
			}
		}
	}
}
