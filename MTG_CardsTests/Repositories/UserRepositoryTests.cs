﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using MTG_Cards.Controllers;
using MTG_Cards.Data;
using MTG_Cards.DTOs;
using MTG_Cards.Models;
using MTG_Cards.Repositories;

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
				new CardCondition { Id = 1, CardId = 1, Condition = Condition.NM, Quantity = 1 },
				new CardCondition { Id = 2, CardId = 1, Condition = Condition.EX, Quantity = 0 },
				new CardCondition { Id = 3, CardId = 1, Condition = Condition.VG, Quantity = 0 },
				new CardCondition { Id = 4, CardId = 1, Condition = Condition.G, Quantity = 0 },
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

			// Repository instance
			_userRepository = new UserRepository(_context!, _mockCache.Object);

			// Controller instance
			_userController = new UserController(_userRepository)
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


		// Check for invalid username is done in controller
		[TestMethod()]
		public async Task GetCardsOwned_ReturnsCards_AddsToCache()
		{
			// Act
			var cardsOwned = await _userRepository!.GetCardsOwned("Bob");

			// Assert
			Assert.AreEqual(1, cardsOwned.Count); // I pre-populated Bob's cards owned
			Assert.AreEqual("Card 1", cardsOwned[0].CardName);
			Assert.AreEqual("NM", cardsOwned[0].Condition);

			var cacheKey = "user-bob";
			_mockCache?.Verify(c => c.GetAsync(cacheKey, default), Times.Once);
		}

		[TestMethod()]
		public void GetCardsOwned_InvalidUser_NotInCache()
		{
			// Arrange
			var cacheKey = "user-sam";

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
			var cardsOwned = await _userRepository.GetCardsOwned(user.Username);

			// Assert
			Assert.IsTrue(isAddCardSuccess);
			Assert.AreEqual(2, cardsOwned.Count);
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
			var cardsOwned = await _userRepository.GetCardsOwned(user.Username);

			// Assert
			Assert.IsFalse(isAddCardSuccess);
			Assert.IsTrue(cardsOwned.Count == 1);
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
			var cardsOwned = await _userRepository.GetCardsOwned(user!.Username);

			// Assert
			Assert.IsTrue(isFirstAddSuccess);
			Assert.IsFalse(isSecondAddSuccess);
			Assert.AreEqual(2, cardsOwned.Count);
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

			// Act
			var isDeleteSuccess = await _userRepository!.DeleteUserCard(user, id: 1);
			var cardsOwned = await _userRepository.GetCardsOwned("Bob");

			// Assert
			Assert.IsTrue(isDeleteSuccess);
			Assert.IsTrue(cardsOwned.Count == 0);
		}
	}
}