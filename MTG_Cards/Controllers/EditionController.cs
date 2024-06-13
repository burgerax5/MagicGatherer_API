using Microsoft.AspNetCore.Mvc;
using MTG_Cards.DTOs;
using MTG_Cards.Interfaces;
using MTG_Cards.Models;
using MTG_Cards.Repositories;
using MTG_Cards.Services.Mappers;

namespace MTG_Cards.Controllers
{
    [ApiController]
    [Route("api/editions")]
    public class EditionController : ControllerBase
    {
        private readonly IEditionRepository _repository;
        public EditionController(IEditionRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public IActionResult GetEditions()
        {
            return Ok(_repository.GetEditions());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetEditionById(int id)
        {
            if (!_repository.EditionExists(id))
                return NotFound("No edition with the id: " + id);

            var edition = await _repository.GetEditionById(id);
            EditionDTO editionDTO = EditionMapper.ToDTO(edition);
			return Ok(editionDTO);
        }

        [HttpGet("search")]
        public IActionResult GetEditionByName(string name)
        {
            if (!_repository.EditionExists(name))
                return NotFound("No edition with the name: " + name);
            return Ok(_repository.GetEditionByName(name));
        }
    }
}
