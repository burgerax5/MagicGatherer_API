﻿namespace MTG_Cards.DTOs
{
	public record struct GroupedEditionNames(
		char header,
		List<EditionDropdownDTO> editions
		);
}
