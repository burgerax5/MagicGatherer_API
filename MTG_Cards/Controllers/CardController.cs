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
        private readonly IEditionRepository _editionRepository;
        public CardController(ICardRepository repository, IEditionRepository editionRepository, IScryfallAPI scryfallAPI)
        {
            _repository = repository;
            _editionRepository = editionRepository;

		}

        [HttpGet]
        public async Task<IActionResult> GetCards([FromQuery] int page=1) // 50 cards per page
        {
            if (page <= 0) return BadRequest("Invalid page");
            return Ok(await _repository.GetCards(page-1));
        }

        [HttpGet("{id}")]
        public IActionResult GetCardById(int id)
        {
            Card card = _repository.GetCardById(id);

            if (card == null)
                return NotFound("No card with the id: " + id);

            CardDTO cardDTO = CardMapper.ToDTO(card);
            return Ok(cardDTO);
        }

        [HttpGet("search")]
        public async Task<IActionResult> GetCardsByName([FromQuery] string name)
        {
            ICollection<CardDTO> cards = await _repository.GetCardsByName(name);

            if (cards.Count == 0)
                return NotFound("No card with the name: " + name);

			return Ok(cards);
        }

   //     [HttpPost]
   //     public IActionResult CreateCard([FromBody] CardCreateDTO request)
   //     {
   //         Edition edition = _editionRepository.GetEditionByName(request.EditionName);

   //         var newCard = new Card
   //         {
   //             Edition = edition,
   //             Name = request.Name,
   //             ImageURL = request.ImageURL,
   //         };

			//foreach (CardConditionDTO condition in request.CardConditions)
   //         {
   //             CardCondition cardCondition = new CardCondition
   //             {
   //                 Condition = (Condition)Enum.Parse(typeof(Condition), condition.Condition),
   //                 Price = condition.Price,
   //                 Quantity = condition.Quantity,
   //             };

   //             if (newCard.Conditions == null)
   //                 newCard.Conditions = new List<CardCondition>();

   //             newCard.Conditions.Add(cardCondition);
   //         }


			//if (_repository.CreateCard(request.EditionName, newCard))
   //             return Created("/" + newCard.Id, "Added the card: " + newCard.Name);
   //         return BadRequest("No edition by the name: " + request.EditionName);
   //     }
    }
}
