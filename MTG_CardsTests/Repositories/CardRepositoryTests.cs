using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using MTG_Cards.Controllers;
using MTG_Cards.Data;
using MTG_Cards.Interfaces;
using MTG_Cards.Models;

namespace MTG_Cards.Repositories.Tests
{
	[TestClass()]
	public class CardRepositoryTests
	{
		private DataContext? _context;
		private Mock<IDistributedCache>? _mockCache;
		private ICardRepository? _cardRepository;
		private CardController? _cardController;

		private Mock<DbSet<Edition>>? _mockEditionSet;
		private Mock<DbSet<Card>>? _mockCardSet;
		private Mock<DbSet<CardCondition>>? _mockCardConditionSet;

		private Mock<HttpContext>? _mockHttpContext;
		private Mock<HttpResponse>? _mockHttpResponse;

		private void MockHttp()
		{
			_mockHttpContext = new Mock<HttpContext>();
			_mockHttpResponse = new Mock<HttpResponse>();

			_mockHttpContext.Setup(c => c.Response).Returns(_mockHttpResponse.Object);
		}
		private void MockDbContext()
		{
			var options = new DbContextOptionsBuilder<DataContext>()
				.UseInMemoryDatabase(databaseName: "TestDatabase")
				.Options;

			_context = new DataContext(options);
		}
		private void ClearDatabase()
		{
			var cardsInDB = _context?.Cards.ToList()!;
			_context?.Cards.RemoveRange(cardsInDB);

			var cardConditionsInDB = _context?.CardConditions.ToList()!;
			_context?.CardConditions.RemoveRange(cardConditionsInDB);

			var editionsInDB = _context?.Editions.ToList()!;
			_context?.Editions.RemoveRange(editionsInDB);
		}
		private List<CardCondition> CreateCardConditions()
		{
			// We will have 51 cards => 51*4 = 204 card conditions
			var conditions = new List<CardCondition>();
			int cardIdCounter = 1;
			for (int i = 1; i < 204; i += 4)
			{
				conditions.Add(new CardCondition { Id = i, CardId = cardIdCounter, Condition = Condition.NM, Quantity = 1 });
				conditions.Add(new CardCondition { Id = i + 1, CardId = cardIdCounter, Condition = Condition.EX, Quantity = 1 });
				conditions.Add(new CardCondition { Id = i + 2, CardId = cardIdCounter, Condition = Condition.VG, Quantity = 1 });
				conditions.Add(new CardCondition { Id = i + 3, CardId = cardIdCounter, Condition = Condition.G, Quantity = 1 });

				cardIdCounter++;
			}
			return conditions;
		}
		private List<Card> CreateCards(List<CardCondition> cardConditions)
		{
			var cards = new List<Card>();
			for (int i = 0; i < 51; i++)
			{
				cards.Add(new Card { 
					Id = i + 1, 
					Name = $"Card {i + 1}", 
					ImageURL = "Image URL", 
					Conditions = cardConditions.Slice(i * 4, 4), 
					NMPrice = i,
					Rarity = i % 2 == 0 ? Rarity.Mythic_Rare : Rarity.Common,
					IsFoil = i > 40
				});
			}
			return cards;
		}
		private void SeedDatabase()
		{
			// Set up editions & cards
			var cardConditions = CreateCardConditions();
			SetupMockDbSet(_mockCardConditionSet!, cardConditions.AsQueryable());

			var cards = CreateCards(cardConditions);
			SetupMockDbSet(_mockCardSet!, cards.AsQueryable());

			var editions = new List<Edition>()
			{
				new Edition { Id = 1, Name = "Edition Name", Code = "edition-code", Cards = cards }
			};
			SetupMockDbSet(_mockEditionSet!, editions.AsQueryable());

			// Populate DB
			_context?.Cards.AddRange(cards);
			_context?.CardConditions.AddRange(cardConditions);
			_context?.Editions.AddRange(editions);
			_context?.SaveChanges();
		}
		private void SetupMockDbSet<T>(Mock<DbSet<T>> mockSet, IQueryable<T> data) where T : class
		{
			mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(data.Provider);
			mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
			mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
			mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
		}
		private void MockDbSet()
		{
			_mockCardSet = new Mock<DbSet<Card>>();
			_mockEditionSet = new Mock<DbSet<Edition>>();
			_mockCardConditionSet = new Mock<DbSet<CardCondition>>();
		}

		[TestInitialize]
		public void Setup()
		{
			MockHttp();
			MockDbContext();
			MockDbSet();

			ClearDatabase();
			SeedDatabase();

			// Mock cache
			_mockCache = new Mock<IDistributedCache>();

			// Repository instance
			_cardRepository = new CardRepository(_context!, _mockCache.Object);

			// Controller instance
			_cardController = new CardController(_cardRepository)
			{
				ControllerContext = new ControllerContext
				{
					HttpContext = _mockHttpContext!.Object
				}
			};
		}

		[TestMethod()]
		public async Task GetCardById_InvalidId()
		{
			// Act
			var card = await _cardRepository!.GetCardById(id: -1);

			// Assert
			Assert.IsNull(card);
		}

		[TestMethod()]
		public async Task GetCardById_ValidId()
		{
			// Act
			var card1 = await _cardRepository!.GetCardById(id: 1);
			var card2 = await _cardRepository.GetCardById(id: 2);

			// Assert
			Assert.IsNotNull(card1);
			Assert.IsNotNull(card2);
			Assert.AreEqual("Card 1", card1?.cardDTO.Name);
			Assert.AreEqual("Card 2", card2?.cardDTO.Name);
		}

		[TestMethod()]
		public async Task GetCards_FirstPage()
		{
			// Act
			var cardPageDTO = await _cardRepository!.GetCards(page: 0, null, null, null, null);

			// Assert
			Assert.AreEqual(2, cardPageDTO.total_pages); // Total of 2 pages
			Assert.AreEqual(1, cardPageDTO.curr_page);  // Current page is the first page

			var cardDTOs = cardPageDTO.CardDTOs;
			Assert.AreEqual(51, cardPageDTO.results); // Total number of results
			Assert.AreEqual(50, cardDTOs.Count); // Results in page

			var firstcard = cardDTOs[0];
			var _50thCard = cardDTOs[49];
			Assert.AreEqual("Card 1", firstcard.Name);
			Assert.AreEqual("Card 50", _50thCard.Name);
		}

		[TestMethod()]
		public async Task GetCards_LastPage()
		{
			// Act
			var cardPageDTO = await _cardRepository!.GetCards(page: 1, null, null, null, null);

			// Assert
			Assert.AreEqual(2, cardPageDTO.curr_page);

			var cardDTOs = cardPageDTO.CardDTOs;
			Assert.AreEqual(51, cardPageDTO.results); // Total number of results
			Assert.AreEqual(1, cardDTOs.Count); // Results in page

			var firstCard = cardDTOs[0];
			Assert.AreEqual("Card 51", firstCard.Name);
		}

		[TestMethod()]
		public async Task GetCards_InvalidPage()
		{
			// Act
			var cardPageDTO = await _cardRepository!.GetCards(page: 2, null, null, null, null);

			// Assert
			var cardDTOs = cardPageDTO.CardDTOs;
			Assert.AreEqual(0, cardDTOs.Count);
		}

		// Check is done on the controller, not repository
		[TestMethod()]
		public async Task GetCards_InvalidPageController()
		{
			// Act
			var actionResult = await _cardController!.GetCards(page: -1);
			var result = actionResult as ObjectResult;

			// Assert
			Assert.AreEqual(StatusCodes.Status400BadRequest, result!.StatusCode);
			Assert.AreEqual("Invalid page", result.Value);
		}

		[TestMethod()]
		public void GetCards_NotCachedResults()
		{
			// Arrange
			string cacheKey = _cardRepository!.GenerateCacheKey(0, null, null, null, null);

			// Assert
			_mockCache?.Verify(c => c.GetAsync(cacheKey, default), Times.Never);
		}

		[TestMethod()]
		public async Task GetCards_ShouldReturnCachedResults()
		{
			// Arrange
			string cacheKey = _cardRepository!.GenerateCacheKey(0, null, null, null, null);

			// Act
			var cardPageDTO = await _cardRepository!.GetCards(0, null, null, null, null); // Will cache

			// Assert
			_mockCache?.Verify(c => c.GetAsync(cacheKey, default), Times.Once);
		}

		[TestMethod()]
		public async Task GetCards_SearchEmptyString_ReturnsAllCards()
		{
			// Arrange
			var search = "";

			// Act
			var cardPageDTO = await _cardRepository!.GetCards(0, search, null, null, null);

			// Assert
			Assert.AreEqual(51, cardPageDTO.results);
		}

		[TestMethod()]
		public async Task GetCards_SearchCommonSubstring()
		{
			// Arrange
			var search = "Card 1";

			// Act
			var cardPageDTO = await _cardRepository!.GetCards(0, search, null, null, null);

			// Assert
			Assert.AreEqual(11, cardPageDTO.results);
		}

		[TestMethod()]
		public async Task GetCards_SearchDistinctSubstring()
		{
			// Arrange
			var search = "NO CARD SHOULD HAVE THIS NAME!";

			// Act
			var cardPageDTO = await _cardRepository!.GetCards(0, search, null, null, null);

			// Assert
			Assert.AreEqual(0, cardPageDTO.results);
		}

		[TestMethod()]
		public async Task GetCards_ValidEditionId()
		{
			// Arrange
			var editionId = 1;

			// Act
			var cardPageDTO = await _cardRepository!.GetCards(0, null, editionId, null, null);

			// Assert
			Assert.AreEqual(51, cardPageDTO.results);
		}

		[TestMethod()]
		public async Task GetCards_InvalidEditionId()
		{
			// Arrange
			var editionId = 2;

			// Act
			var cardPageDTO = await _cardRepository!.GetCards(0, null, editionId, null, null);

			// Assert
			Assert.AreEqual(0, cardPageDTO.results);
		}

		[TestMethod()]
		public async Task GetCards_SortByName()
		{
			// Act
			var sortByNameAscending = await _cardRepository!.GetCards(1, null, null, "name_asc", null);
			var sortByNameDescending = await _cardRepository!.GetCards(1, null, null, "name_desc", null);

			// Assert
			var lastCardNameAscending = sortByNameAscending.CardDTOs.First().Name;
			Assert.AreEqual("Card 9", lastCardNameAscending);

			var lastCardnameDescending = sortByNameDescending.CardDTOs.First().Name;
			Assert.AreEqual("Card 1", lastCardnameDescending);
		}

		[TestMethod()]
		public async Task GetCards_SortByPrice()
		{
			// Act
			var sortByPriceAscending = await _cardRepository!.GetCards(0, null, null, "price_asc", null);
			var sortByPriceDescending = await _cardRepository!.GetCards(0, null, null, "price_desc", null);

			// Assert
			var cheapestCard = sortByPriceAscending.CardDTOs.First();
			Assert.AreEqual(0, cheapestCard.NMPrice);

			var mostExpensiveCard = sortByPriceDescending.CardDTOs.First();
			Assert.AreEqual(50, mostExpensiveCard.NMPrice);
		}

		[TestMethod()]
		public async Task GetCards_SortByRarity()
		{
			// Act
			var sortByRarityAscending = await _cardRepository!.GetCards(0, null, null, "rarity_asc", null);
			var sortByRarityDescending = await _cardRepository!.GetCards(0, null, null, "rarity_desc", null);

			// Assert
			var lowestRarity = sortByRarityAscending.CardDTOs.First();
			Assert.AreEqual(Rarity.Common, lowestRarity.Rarity);

			var highestRarity = sortByRarityDescending.CardDTOs.First();
			Assert.AreEqual(Rarity.Mythic_Rare, highestRarity.Rarity);
		}

		[TestMethod()]
		public async Task GetCards_FoilFilter()
		{
			// Act
			var foilsOnly = await _cardRepository!.GetCards(0, null, null, null, "foils_only");
			var hideFoils = await _cardRepository!.GetCards(0, null, null, null, "hide_foils");

			// Assert
			Assert.AreEqual(10, foilsOnly.results); // 10 foils
			Assert.AreEqual(41, hideFoils.results); // 41 non-foils
		}
	}
}