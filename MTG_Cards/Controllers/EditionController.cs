﻿using Microsoft.AspNetCore.Mvc;
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
        public async Task<IActionResult> GetEditionNames()
        {
            return Ok(await _repository.GetEditionNames());
        }

        [HttpGet("grouped")]
        public async Task<IActionResult> GetEditionNamesGrouped()
        {
            return Ok(await _repository.GetEditionNamesGrouped());
        }

        [HttpGet("dropdown")]
        public async Task<IActionResult> GetEditionsDropdown()
        {
            return Ok(await _repository.GetEditionsDropdown());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetEditionById(int id)
        {
            var editionDTO = await _repository.GetEditionById(id);
			if (editionDTO == null)
				return NotFound("No edition with the id: " + id);

			return Ok(editionDTO);
        }
    }
}
