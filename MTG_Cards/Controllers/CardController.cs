using Microsoft.AspNetCore.Mvc;
using MTG_Cards.DTOs;
using MTG_Cards.Interfaces;
using MTG_Cards.Models;
using MTG_Cards.Repositories;
using MTG_Cards.Services;
using MTG_Cards.Services.Mappers;

namespace MTG_Cards.Controllers
{
    [ApiController]
    [Route("api/cards")]
    public class CardController : ControllerBase
    {
        private readonly ICardRepository _repository;
        public CardController(ICardRepository repository)
        {
            _repository = repository;
		}

        [HttpGet]
        public async Task<IActionResult> GetCards(
            [FromQuery] int page=1,
            [FromQuery] string? search=null,
            [FromQuery] int? editionId=null,
            [FromQuery] string? sortBy=null,
            [FromQuery] string? foilFilter=null) // 50 cards per page
        {
            if (page <= 0) return BadRequest("Invalid page");
            try
            {
				var cardPageDTO = await _repository.GetCards(page - 1, search, editionId, sortBy, foilFilter);
				return Ok(cardPageDTO);
			} catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCardById(int id)
        {
            CardDetailedDTO? cardDTO = await _repository.GetCardById(id);

            if (cardDTO == null)
                return NotFound("No card with the id: " + id);

            return Ok(cardDTO);
        }
    }
}
