using MTG_Cards.Models;

namespace MTG_Cards.Interfaces
{
	public interface IScryfallAPI
	{
		Task<List<string>> GetCardNames();
		Task<string> GetCardsJson();
		Task<ScryfallCard> DeserializeCardNames(string json);
		void DecodeCardNames(ScryfallCard scyfallCard);
	}
}
