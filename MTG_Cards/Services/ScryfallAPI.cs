using MTG_Cards.Interfaces;
using MTG_Cards.Models;
using System.Net;
using System.Text.Json;

namespace MTG_Cards.Services
{
	public class ScryfallAPI : IScryfallAPI
	{
		private readonly IHttpClientFactory _httpClientFactory;

		public ScryfallAPI(IHttpClientFactory httpClientFactory)
        {
			_httpClientFactory = httpClientFactory;
		}

		public async Task<List<string>> GetCardNames()
		{
			var cardsJson = await GetCardsJson();
			var scryfallCards = await DeserializeCardNames(cardsJson);
			DecodeCardNames(scryfallCards);

			return scryfallCards.Data;
		}

        public async Task<string> GetCardsJson()
		{
			var client = _httpClientFactory.CreateClient();
			string url = "https://api.scryfall.com/catalog/card-names";
			var response = await client.GetAsync(url);
			response.EnsureSuccessStatusCode();
			return await response.Content.ReadAsStringAsync();
		}

		public async Task<ScryfallCard> DeserializeCardNames(string json)
		{
			var scryfallCards = JsonSerializer.Deserialize<ScryfallCard>(json, new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			});
			return scryfallCards;
		}

		public void DecodeCardNames(ScryfallCard scryfallCard)
		{
			List<string> cardNames = scryfallCard.Data;
			List<string> decodedCardNames = new List<string>();
			foreach (var cardName in cardNames)
			{
				var bruh = cardName.IndexOf("\"");
				string updatedCardName = cardName.
					Replace("A-", "").
					Replace(" . . .", "...");

				decodedCardNames.Add(updatedCardName);
			}

			scryfallCard.Data = decodedCardNames;
		}
	}
}
