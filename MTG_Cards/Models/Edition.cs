using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace MTG_Cards.Models
{
    public class Edition
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Code { get; set; }
		public ICollection<Card> Cards { get; set; } = new List<Card>();
	}
}
