namespace MTG_Cards.DTOs
{
    public record struct CardCreateDTO(
        string EditionName,
        string Name, 
        string ImageURL,
        List<CardConditionDTO> CardConditions);
}
