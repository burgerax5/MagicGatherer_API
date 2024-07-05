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
        public async Task<IActionResult> GetCards([FromQuery] int page=1) // 50 cards per page
        {
            if (page <= 0) return BadRequest("Invalid page");
            return Ok(await _repository.GetCards(page-1));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCardById(int id)
        {
            CardDTO? cardDTO = await _repository.GetCardById(id);

            if (cardDTO == null)
                return NotFound("No card with the id: " + id);

            return Ok(cardDTO);
        }

   //     [HttpGet("search")]
   //     public async Task<IActionResult> GetCardsByName([FromQuery] string name)
   //     {
			//List<CardDTO> cards = await _repository.GetCardsByName(name);

   //         if (cards.Count == 0)
   //             return NotFound("No card with the name: " + name);

			//return Ok(cards);
   //     }
    }
}
