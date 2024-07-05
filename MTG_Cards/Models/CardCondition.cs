using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Text.Json.Serialization;

namespace MTG_Cards.Models
{
	public class CardCondition
	{
		public int Id { get; set; }
		public int CardId { get; set; }
		public required Card? Card { get; set; }
		public int Quantity { get; set; }
		public Condition Condition { get; set; }
		public double Price { get; set; }
	}
}
