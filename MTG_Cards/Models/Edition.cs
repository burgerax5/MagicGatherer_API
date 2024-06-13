using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace MTG_Cards.Models
{
    public class Edition
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public ICollection<Card> Cards { get; set; }
    }
}
