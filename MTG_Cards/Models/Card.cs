using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MTG_Cards.Models
{
	public enum Rarity
	{
		Common,
		Uncommon,
		Rare,
		Mythic_Rare
	};

	public class Card
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string ImageURL { get; set; }
        public int EditionId {  get; set; }
        public required Edition Edition { get; set; }
		public Rarity Rarity { get; set; }
		public List<CardCondition> Conditions { get; set; } = new List<CardCondition>();
        public bool IsFoil { get; set; }
    }
}
