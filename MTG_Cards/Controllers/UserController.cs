using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
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
		private readonly MailService _mailService;
		public UserController(IUserRepository repository, MailService mailService)
		{
			_repository = repository;
			_mailService = mailService;
		}

		[HttpGet("cards/{username}")]
		public async Task<IActionResult> GetUserCards(
			string username,
			[FromQuery] int page = 1,
			[FromQuery] string? search = null,
			[FromQuery] int? editionId = null,
			[FromQuery] string? sortBy = null,
			[FromQuery] string? foilFilter = null)
		{
			var userExists = _repository.UserExists(username);
			if (!userExists)
				return NotFound($"User: {username} not found");

			CardPageDTO cardsPageDTO = await _repository.GetCardsOwned(username, page - 1, search, editionId, sortBy, foilFilter);
			(int totalCards, double totalValue) = await _repository.GetTotalCardsAndValue(username);

			CardsOwnedPageDTO cardsOwnedPage = new CardsOwnedPageDTO(
				totalCards,
				double.Round(totalValue, 2, MidpointRounding.AwayFromZero),
				cardsPageDTO);

			return Ok(cardsOwnedPage);
		}

		[HttpGet("cards/{username}/details")]
		public async Task<IActionResult> GetUserCollectionDetails(string username)
		{
			var userExists = _repository.UserExists(username);
			if (!userExists)
				return NotFound($"User: {username} not found");

			(int totalCards, double totalValue) = await _repository.GetTotalCardsAndValue(username);
			CollectDetailsDTO collectionDetailsDTO = new CollectDetailsDTO(totalCards, totalValue);
			return Ok(collectionDetailsDTO);
		}

		[HttpGet("cards/conditions/{cardId}")]
		[Authorize]
		public async Task<IActionResult> GetUserCardsConditionsOwned(int cardId)
		{
			var username = User?.Identity?.Name;
			if (string.IsNullOrEmpty(username))
				return BadRequest("Username cannot be null");

			var conditions = await _repository.GetCardConditions(username, cardId);
			return Ok(conditions);
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

		[HttpPost("forgot-password")]
		public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
		{
			var email = request.Email;
			var resetToken = await _repository.CreatePasswordResetToken(email);

			if (resetToken == null) return BadRequest("Invalid reset token");
			var resetPasswordLink = $"https://magicgatherer.netlify.app/reset-password?token={resetToken}";

			// Send email
			await _mailService.SendPasswordResetEmail(email, resetPasswordLink);
			return Ok("Password reset email sent");
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
				var keyEncoded = Encoding.ASCII.GetBytes("wUAlIcbfF97TuJe78ocQr55JF9Tf7BaoP9aHYU9qZg8");
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
