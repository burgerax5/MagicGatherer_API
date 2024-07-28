using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Moq;
using MTG_Cards.Controllers;
using MTG_Cards.Data;
using MTG_Cards.DTOs;
using MTG_Cards.Interfaces;
using MTG_Cards.Models;
using MTG_Cards.Repositories;
using MTG_Cards.Services;
using StackExchange.Redis;
using System.Globalization;

namespace MTG_CardsTests.Repositories
{
	[TestClass()]
	public class UserRepositoryTests
	{
		private DataContext? _context;
		private Mock<IDistributedCache>? _mockCache;
		private UserRepository? _userRepository;

		private Mock<DbSet<User>>? _mockUserSet;
		private Mock<DbSet<Edition>>? _mockEditionSet;
		private Mock<DbSet<Card>>? _mockCardSet;
		private Mock<DbSet<CardCondition>>? _mockCardConditionSet;
		private Mock<DbSet<CardOwned>>? _mockCardOwnedSet;

		private UserController? _userController;
		private Mock<HttpContext>? _mockHttpContext;
		private Mock<HttpResponse>? _mockHttpResponse;
		private Mock<ICacheHelper>? _mockCacheHelper;
		private Mock<IConnectionMultiplexer>? _mockConnectionMultiplexer;
		private Mock<MailService>? _mockMailService;
		private Mock<IConfiguration>? _mockConfiguration;

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
		private void MockDbSet()
		{
			_mockUserSet = new Mock<DbSet<User>>();
			_mockCardSet = new Mock<DbSet<Card>>();
			_mockEditionSet = new Mock<DbSet<Edition>>();
			_mockCardConditionSet = new Mock<DbSet<CardCondition>>();
			_mockCardOwnedSet = new Mock<DbSet<CardOwned>>();
		}
		private void ClearDatabase()
		{
			_context!.Users.RemoveRange(_context.Users);
			_context.Cards.RemoveRange(_context.Cards);
			_context.CardsOwned.RemoveRange(_context.CardsOwned);
			_context.CardConditions.RemoveRange(_context.CardConditions);
			_context.Editions.RemoveRange(_context.Editions);
			_context.SaveChanges();
		}
		private void SeedDatabase()
		{
			// Set up user
			var users = new List<User>()
			{
				new User {
					Id = 1,
					Username = "Bob",
					Password = "/T8mzUQsyZVetbsJ5sdzIbJpaN2Aip/gQop6gK4CU/A=",
					Salt = "buTrO8xGUypUedC4t7lV6w==" }
			};
			SetupMockDbSet(_mockUserSet!, users.AsQueryable());


			// Set up editions & cards
			var cardConditions = new List<CardCondition>()
			{
				new CardCondition { Id = 1, CardId = 1, Condition = MTG_Cards.Models.Condition.NM, Quantity = 1, Price = 0.4 },
				new CardCondition { Id = 2, CardId = 1, Condition = MTG_Cards.Models.Condition.EX, Quantity = 0, Price = 0.3 },
				new CardCondition { Id = 3, CardId = 1, Condition = MTG_Cards.Models.Condition.VG, Quantity = 0, Price = 0.2 },
				new CardCondition { Id = 4, CardId = 1, Condition = MTG_Cards.Models.Condition.G, Quantity = 0, Price = 0.1 },
			};
			SetupMockDbSet(_mockCardConditionSet!, cardConditions.AsQueryable());

			var cardsOwned = new List<CardOwned>()
			{
				new CardOwned { Id = 1, CardConditionId = 1, Quantity = 2, UserId = 1 }
			};
			SetupMockDbSet(_mockCardOwnedSet!, cardsOwned.AsQueryable());

			var cards = new List<Card>()
			{
				new Card { Id = 1, Name = "Card 1", ImageURL = "Image URL", Conditions = cardConditions, Rarity = Rarity.Common },
			};
			SetupMockDbSet(_mockCardSet!, cards.AsQueryable());

			var editions = new List<Edition>()
			{
				new Edition { Id = 1, Name = "Edition Name", Code = "edition-code", Cards = cards }
			};
			SetupMockDbSet(_mockEditionSet!, editions.AsQueryable());

			// Populate DB
			_context!.Users.AddRange(users);
			_context.Cards.AddRange(cards);
			_context.CardConditions.AddRange(cardConditions);
			_context.CardsOwned.AddRange(cardsOwned);
			_context.Editions.AddRange(editions);
			_context.SaveChanges();
		}
		private void SetupMockDbSet<T>(Mock<DbSet<T>> mockSet, IQueryable<T> data) where T : class
		{
			mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(data.Provider);
			mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
			mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
			mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
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
			_mockCacheHelper = new Mock<ICacheHelper>(); // Mocking the interface

			var mockServer = new Mock<IServer>();
			var mockDatabase = new Mock<IDatabase>();
			_mockConnectionMultiplexer = new Mock<IConnectionMultiplexer>();
			_mockConfiguration = new Mock<IConfiguration>();
			_mockMailService = new Mock<MailService>(_mockConfiguration.Object);

			_mockConnectionMultiplexer.Setup(m => m.GetServer(It.IsAny<string>(), It.IsAny<object>()))
			.Returns(mockServer.Object);
			_mockConnectionMultiplexer.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
				.Returns(mockDatabase.Object);

			// Repository instance
			_userRepository = new UserRepository(_context!, _mockCache.Object, _mockCacheHelper.Object);

			// Controller instance
			_userController = new UserController(_userRepository, _mockMailService.Object)
			{
				ControllerContext = new ControllerContext
				{
					HttpContext = _mockHttpContext!.Object
				}
			};
		}

		[TestMethod()]
		public void GetUserById_ValidId()
		{
			// Arrange
			int id = 1;
			var username = "Bob";

			// Act
			var result = _userRepository!.GetUserById(id);

			// Assert
			Assert.IsNotNull(result);
			Assert.AreEqual(id, result.Id);
			Assert.AreEqual(username, result.Username);
		}


		[TestMethod()]
		public void GetUserById_InvalidId()
		{
			// Arrange
			int id = 2;

			// Act
			var result = _userRepository!.GetUserById(id);

			// Assert
			Assert.IsNull(result);
		}


		[TestMethod()]
		public void GetUserByUsername_ValidUsername()
		{
			// Arrange
			string username = "Bob";

			// Act
			var result = _userRepository!.GetUserByUsername(username);

			// Assert
			Assert.IsNotNull(result);
			Assert.AreEqual(1, result.Id);
			Assert.AreEqual("Bob", result.Username);
		}


		[TestMethod()]
		public void GetUserByUsername_InvalidUsername()
		{
			// Arrange
			string username = "Sam";

			// Act
			var result = _userRepository!.GetUserByUsername(username);

			// Assert
			Assert.IsNull(result);
		}


		[TestMethod()]
		public void RegisterUser_Success()
		{
			// Arrange
			UserLoginDTO userLoginDTO = new UserLoginDTO
			{
				Username = "Sam",
				Password = "Sam's Password"
			};

			// Act
			_userRepository!.RegisterUser(userLoginDTO);
			var userSam = _userRepository.GetUserByUsername("Sam");

			// Assert
			Assert.IsNotNull(userSam);
			Assert.AreEqual("Sam", userSam.Username);
		}

		[TestMethod()]
		public void RegisterUser_UserAlreadyExists()
		{
			// Arrange
			UserLoginDTO userLoginDTO = new UserLoginDTO
			{
				Email = "sam@gmail.com",
				Username = "Sam",
				Password = "Sam's Password"
			};

			// Act
			var samActionResult1 = _userController!.RegisterUser(userLoginDTO);
			var samActionResult2 = _userController.RegisterUser(userLoginDTO); // Registering Sam again
			var result1 = samActionResult1 as ObjectResult;
			var result2 = samActionResult2 as ObjectResult;

			// Assert
			Assert.AreEqual(StatusCodes.Status200OK, result1?.StatusCode);
			Assert.AreEqual(StatusCodes.Status400BadRequest, result2?.StatusCode);
		}


		[TestMethod()]
		public void LoginUser_Success()
		{
			// Arrange
			UserLoginDTO userLoginDTO = new UserLoginDTO
			{
				Username = "Bob",
				Password = "bruh"
			};

			// Act
			var loginSuccess = _userRepository!.LoginUser(userLoginDTO);

			// Assert
			Assert.IsTrue(loginSuccess);
		}


		[TestMethod()]
		public void LoginUser_UserNotFound()
		{
			// Arrange
			UserLoginDTO userLoginDTO = new UserLoginDTO
			{
				Username = "Sam",
				Password = "Sam's Password"
			};

			// Act
			var loginSuccess = _userRepository!.LoginUser(userLoginDTO);

			// Assert
			Assert.IsFalse(loginSuccess);
		}


		[TestMethod()]
		public void LoginUser_InvalidPassword()
		{
			// Arrange
			UserLoginDTO userLoginDTO = new UserLoginDTO
			{
				Username = "Bob",
				Password = "Incorrect Password"
			};

			// Act
			var loginSuccess = _userRepository!.LoginUser(userLoginDTO);

			// Assert
			Assert.IsFalse(loginSuccess);
		}

		[TestMethod()]
		public async Task GetTotalCardsAndValue_ValidUsername()
		{
			// Arrange
			var username = "Bob";

			// Act
			(var totalCards, var totalValue) = await _userRepository!.GetTotalCardsAndValue(username);

			// Assert
			Assert.AreEqual(2, totalCards);
			Assert.AreEqual(0.8, totalValue); // 2 cards each worth $0.4
		}

		[TestMethod()]
		public async Task GetTotalCardsAndValue_InvalidUsername()
		{
			// Arrange
			var username = "Sam";

			// Act
			(var totalCards, var totalValue) = await _userRepository!.GetTotalCardsAndValue(username);

			// Assert
			Assert.AreEqual(-1, totalCards);
			Assert.AreEqual(-1, totalValue);
		}

		[TestMethod()]
		public void GenerateCacheKey_ReturnsString()
		{
			// Arrange
			var username = "Bob";
			var page = 0; // 0 would be the first page
			var search = "Air";
			var editionId = 1;
			var sortBy = "name_asc";
			var foilFilter = "foils_only";

			// Act
			var cacheKey = _userRepository!.GenerateCacheKey(username, page, search, editionId, sortBy, foilFilter);

			// Assert
			var expectedKey = "user_bob_cards_page_0_search_Air_edition_1_sort_name_asc_foilFilter_foils_only";
			Assert.AreEqual(expectedKey, cacheKey);
		}

		// Check for invalid username is done in controller
		[TestMethod()]
		public async Task GetCardsOwned_ReturnsCards_AddsToCache()
		{
			// Act
			var cardsOwned = await _userRepository!.GetCardsOwned("Bob", 0, null, null, null, null);

			// Assert
			Assert.AreEqual(1, cardsOwned.results); // I pre-populated Bob's cards owned
			Assert.AreEqual("Card 1", cardsOwned.CardDTOs[0].Name);

			var cacheKey = "user_bob_cards_page_0_search_none_edition_none_sort_none_foilFilter_none";
			_mockCache?.Verify(c => c.GetAsync(cacheKey, default), Times.Once);
		}

		[TestMethod()]
		public void GetCardsOwned_InvalidUser_NotInCache()
		{
			// Arrange
			var cacheKey = "user_sam_page_page_1_search_none_edition_none_sort_none_foilFilter_none";

			// Assert
			_mockCache?.Verify(c => c.GetAsync(cacheKey, default), Times.Never);
		}


		[TestMethod()]
		public async Task AddCardsOwned_ValidCardCondition()
		{
			// Arrange
			var user = await _context!.Users.Include(u => u.CardsOwned).FirstAsync();
			var cardOwned = new CreateCardOwnedDTO
			{
				CardId = 1,
				Condition = "VG",
				Quantity = 1,
			};

			// Act
			var isAddCardSuccess = await _userRepository!.AddUserCard(user, cardOwned);
			var cardsOwned = await _userRepository.GetTotalCardsAndValue(user.Username);

			// Assert
			Assert.IsTrue(isAddCardSuccess);
			Assert.AreEqual(3, cardsOwned.totalCards);
		}


		[TestMethod()]
		public async Task AddCardsOwned_InvalidCardCondition()
		{
			// Arrange
			var user = await _context!.Users.Include(u => u.CardsOwned).FirstAsync();
			var cardOwned = new CreateCardOwnedDTO
			{
				CardId = 2,
				Condition = "VG",
				Quantity = 1,
			};

			// Act
			var isAddCardSuccess = await _userRepository!.AddUserCard(user, cardOwned);
			var cardsOwned = await _userRepository.GetCardsOwned(user.Username, 1, null, null, null, null);

			// Assert
			Assert.IsFalse(isAddCardSuccess);
			Assert.IsTrue(cardsOwned.results == 1);
		}


		[TestMethod()]
		public async Task AddCardsOwned_ExistingCardCondition_DontAddTwice()
		{
			// Arrange
			var user = await _context!.Users.Include(u => u.CardsOwned).FirstAsync();
			Assert.IsNotNull(user);
			var cardOwned = new CreateCardOwnedDTO
			{
				CardId = 1,
				Condition = "VG",
				Quantity = 1,
			};

			// Act
			var isFirstAddSuccess = await _userRepository!.AddUserCard(user, cardOwned);
			var isSecondAddSuccess = await _userRepository.AddUserCard(user, cardOwned);
			var collectionDetails = await _userRepository.GetTotalCardsAndValue(user.Username);

			// Assert
			Assert.IsTrue(isFirstAddSuccess);
			Assert.IsFalse(isSecondAddSuccess);
			Assert.AreEqual(3, collectionDetails.totalCards);
		}


		[TestMethod()]
		public async Task UpdateUserCard_InvalidCardId()
		{
			// Arrange
			var user = _context!.Users.First();
			UpdateCardOwnedDTO updateCardOwned = new UpdateCardOwnedDTO { Quantity = 1 };

			// Act
			var isUpdateSuccess = await _userRepository!.UpdateUserCard(user, id: 2, updateCardOwned);

			// Assert
			Assert.IsFalse(isUpdateSuccess);
		}


		[TestMethod()]
		public async Task UpdateUserCard_ValidCardId_WrongUser()
		{
			// Arrange
			var user = new User { Id = 2, Username = "Speedwagon", Password = "PW", Salt = "Salt" };
			UpdateCardOwnedDTO updateCardOwned = new UpdateCardOwnedDTO { Quantity = 1 };

			// Act
			var isUpdateSuccess = await _userRepository!.UpdateUserCard(user, id: 1, updateCardOwned);

			// Assert
			Assert.IsFalse(isUpdateSuccess);
		}


		[TestMethod()]
		public async Task UpdateUserCard_Success()
		{
			// Arrange
			var user = await _context!.Users.Include(u => u.CardsOwned).FirstAsync();
			UpdateCardOwnedDTO updateCardOwned = new UpdateCardOwnedDTO { Quantity = 3 };

			// Act
			var isUpdateSuccess = await _userRepository!.UpdateUserCard(user, id: 1, updateCardOwned);
			var updatedCardOwned = _context!.CardsOwned.First();

			// Assert
			Assert.IsTrue(isUpdateSuccess);
			Assert.IsNotNull(updatedCardOwned);
			Assert.AreEqual(3, updatedCardOwned.Quantity);
		}

		[TestMethod()]
		public async Task DeleteUserCard_InvalidId()
		{
			// Arrange
			var user = await _context!.Users.Include(u => u.CardsOwned).FirstAsync();

			// Act
			var isDeleteSuccess = await _userRepository!.DeleteUserCard(user, id: 2);

			// Assert
			Assert.IsFalse(isDeleteSuccess);
		}


		[TestMethod()]
		public async Task DeleteUserCard_ValidId_InvalidUser()
		{
			// Arrange
			var user = new User { Id = 2, Username = "Speedwagon", Password = "PW", Salt = "Salt" };

			// Act
			var isDeleteSuccess = await _userRepository!.DeleteUserCard(user, id: 2);

			// Assert
			Assert.IsFalse(isDeleteSuccess);
		}


		[TestMethod()]
		public async Task DeleteUserCard_Success()
		{
			// Arrange
			var user = await _context!.Users.Include(u => u.CardsOwned).FirstAsync();
			var username = "Bob";

			// Act
			var isDeleteSuccess = await _userRepository!.DeleteUserCard(user, id: 1);
			var collectionDetails = await _userRepository.GetTotalCardsAndValue(username);

			// Assert
			Assert.IsTrue(isDeleteSuccess);
			Assert.IsTrue(collectionDetails.totalCards == 0);
		}
	}
}
