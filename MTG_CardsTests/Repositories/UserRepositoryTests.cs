using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MTG_Cards.Controllers;
using MTG_Cards.Data;
using MTG_Cards.DTOs;
using MTG_Cards.Interfaces;
using MTG_Cards.Models;
using MTG_Cards.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MTG_Cards.Repositories.Tests
{
	[TestClass()]
	public class UserRepositoryTests
	{
		private DataContext _context;
		private Mock<IDistributedCache> _mockCache;
		private UserRepository _userRepository;

		private Mock<DbSet<User>> _mockUserSet;
		private Mock<DbSet<Edition>> _mockEditionSet;
		private Mock<DbSet<Card>> _mockCardSet;
		private Mock<DbSet<CardCondition>> _mockCardConditionSet;
		private Mock<DbSet<CardOwned>> _mockCardOwnedSet;

		private UserController _userController;
		private Mock<HttpContext> _mockHttpContext;
		private Mock<HttpResponse> _mockHttpResponse;
		private Mock<IResponseCookies> _mockResponseCookies;

		private void MockHttp()
		{
			_mockHttpContext = new Mock<HttpContext>();
			_mockHttpResponse = new Mock<HttpResponse>();
			_mockResponseCookies = new Mock<IResponseCookies>();

			_mockHttpResponse.Setup(r => r.Cookies).Returns(_mockResponseCookies.Object);
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
			var usersInDB = _context.Users.ToList();
			_context.Users.RemoveRange(usersInDB);

			var cardsInDB = _context.Cards.ToList();
			_context.Cards.RemoveRange(cardsInDB);

			var cardsOwnedInDB = _context.CardsOwned.ToList();
			_context.CardsOwned.RemoveRange(cardsOwnedInDB);

			var cardConditionsInDB = _context.CardConditions.ToList();
			_context.CardConditions.RemoveRange(cardConditionsInDB);

			var editionsInDB = _context.Editions.ToList();
			_context.Editions.RemoveRange(editionsInDB);
		}

		private void SeedDatabase()
		{
			// Set up user
			var users = new List<User>()
			{
				new User { Id = 1, Username = "Bob", Password = "Bob's Password", Salt = "Salt" }
			};
			SetupMockDbSet(_mockUserSet, users.AsQueryable());


			// Set up editions & cards
			var cardConditions = new List<CardCondition>()
			{
				new CardCondition { Id = 1, CardId = 1, Condition = Condition.NM, Quantity = 1 },
				new CardCondition { Id = 2, CardId = 1, Condition = Condition.EX, Quantity = 0 },
				new CardCondition { Id = 3, CardId = 1, Condition = Condition.VG, Quantity = 0 },
				new CardCondition { Id = 4, CardId = 1, Condition = Condition.G, Quantity = 0 },
				new CardCondition { Id = 5, CardId = 2, Condition = Condition.NM, Quantity = 1 },
				new CardCondition { Id = 6, CardId = 2, Condition = Condition.EX, Quantity = 0 },
				new CardCondition { Id = 7, CardId = 2, Condition = Condition.VG, Quantity = 0 },
				new CardCondition { Id = 8, CardId = 2, Condition = Condition.G, Quantity = 0 },
			};
			SetupMockDbSet(_mockCardConditionSet, cardConditions.AsQueryable());

			var cardsOwned = new List<CardOwned>()
			{
				new CardOwned { Id = 1, CardConditionId = 1, Quantity = 2, User = users[0] }
			};
			SetupMockDbSet(_mockCardOwnedSet, cardsOwned.AsQueryable());

			var cards = new List<Card>()
			{
				new Card { Id = 1, Name = "Card 1", ImageURL = "Image URL", Conditions = cardConditions },
				new Card { Id = 2, Name = "Card 2", ImageURL = "Image URL" }
			};
			SetupMockDbSet(_mockCardSet, cards.AsQueryable());

			var editions = new List<Edition>()
			{
				new Edition { Id = 1, Name = "Edition Name", Code = "edition-code", Cards = cards }
			};
			SetupMockDbSet(_mockEditionSet, editions.AsQueryable());

			// Populate DB
			_context.Users.AddRange(users);
			_context.Cards.AddRange(cards);
			_context.CardConditions.AddRange(cardConditions);
			_context.CardsOwned.AddRange(cardsOwned);
			_context.Editions.AddRange(editions);
			_context.SaveChanges();
		}

		[TestInitialize]
		public void Setup()
		{
			MockHttp();
			MockDbContext();
			MockDbSet();

			Environment.SetEnvironmentVariable("SECRET_KEY", "super-secret-key");

			ClearDatabase();
			SeedDatabase();

			// Mock cache
			_mockCache = new Mock<IDistributedCache>();

			// Repository instance
			_userRepository = new UserRepository(_context, _mockCache.Object);

			// Controller instance
			_userController = new UserController(_userRepository)
			{
				ControllerContext = new ControllerContext
				{
					HttpContext = _mockHttpContext.Object
				}
			};
		}


		private void SetupMockDbSet<T>(Mock<DbSet<T>> mockSet, IQueryable<T> data) where T : class
		{
			mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(data.Provider);
			mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
			mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
			mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
		}


		[TestMethod()]
		public void GetUserById_ValidId()
		{
			// Arrange
			int id = 1;
			var expectedUser = new User { Id = id, Username = "Bob", Password = "Bob's Password" };

			// Act
			var result = _userRepository.GetUserById(id);

			// Assert
			Assert.IsNotNull(result);
			Assert.AreEqual(id, result.Id);
			Assert.AreEqual(expectedUser.Username, result.Username);
		}


		[TestMethod()]
		public void GetUserById_InvalidId()
		{
			// Arrange
			int id = 2;
			var expectedUser = new User { Id = id, Username = "Bob", Password = "Bob's Password" };

			// Act
			var result = _userRepository.GetUserById(id);

			// Assert
			Assert.IsNull(result);
		}


		[TestMethod()]
		public void GetUserByUsername_ValidUsername()
		{
			// Arrange
			string username = "Bob";
			var expectedUser = new User { Id = 1, Username = username, Password = "Bob's Password" };

			// Act
			var result = _userRepository.GetUserByUsername(username);

			// Assert
			Assert.IsNotNull(result);
			Assert.AreEqual(expectedUser.Id, result.Id);
			Assert.AreEqual(username, result.Username);
			Assert.AreEqual("Bob's Password", result.Password);
		}


		[TestMethod()]
		public void GetUserByUsername_InvalidUsername()
		{
			// Arrange
			string username = "Sam";

			// Act
			var result = _userRepository.GetUserByUsername(username);

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
			_userRepository.RegisterUser(userLoginDTO);
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
			var samActionResult1 = _userController.RegisterUser(userLoginDTO);
			var samActionResult2 = _userController.RegisterUser(userLoginDTO); // Registering Sam again
			var result1 = samActionResult1 as ObjectResult;
			var result2 = samActionResult2 as ObjectResult;

			// Assert
			Assert.AreEqual(StatusCodes.Status200OK, result1.StatusCode);
			Assert.AreEqual(StatusCodes.Status400BadRequest, result2.StatusCode);
		}


		[TestMethod()]
		public void LoginUser_Success()
		{
			// Arrange
			UserLoginDTO userLoginDTO = new UserLoginDTO
			{
				Username = "Sam",
				Password = "Sam's Password"
			};

			// Act
			var registerActionResult = _userController.RegisterUser(userLoginDTO);
			var loginActionResult = _userController.LoginUser(userLoginDTO);
			var loginResult = loginActionResult as ObjectResult;

			// Assert
			Assert.AreEqual(StatusCodes.Status200OK, loginResult.StatusCode);
			Assert.AreEqual("Successfully logged in", loginResult.Value);

			// Verify cookie was set
			_mockResponseCookies.Verify(
				c => c.Append(
					"auth",
					It.Is<string>(s => s.StartsWith("Sam.")),
					It.IsAny<CookieOptions>()),
					Times.Once
				);
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
			var loginActionResult = _userController.LoginUser(userLoginDTO);
			var loginResult = loginActionResult as ObjectResult;

			// Assert
			Assert.AreEqual(StatusCodes.Status404NotFound, loginResult.StatusCode);
			Assert.AreEqual("User not found", loginResult.Value);
		}


		[TestMethod()]
		public void LoginUser_InvalidPassword()
		{
			// Arrange
			UserLoginDTO userLoginDTO = new UserLoginDTO
			{
				Username = "Bob",
				Password = "Bob's Password" // Stored password is not hashed, but the login attempt hashes password
			};

			// Act
			var loginActionResult = _userController.LoginUser(userLoginDTO);
			var loginResult = loginActionResult as ObjectResult;

			// Assert
			Assert.AreEqual(StatusCodes.Status400BadRequest, loginResult.StatusCode);
			Assert.AreEqual("Invalid user credentials", loginResult.Value);
		}


		[TestMethod()]
		public async Task GetCardsOwned_ValidUser_NoCards()
		{
			// Arrange
			var username = "Bob";

			// Act
			var cardsOwned = await _userRepository.GetCardsOwned(username);

			// Assert
			Assert.AreEqual(0, cardsOwned.Count);
		}


		[TestMethod()]
		public async Task AddCardsOwned_ValidCardCondition()
		{
			// Arrange
			var user = new User { Id = 1, Username = "Bob" };
			var cardOwned = new CreateCardOwnedDTO 
			{ 
				CardId = 1,
				Condition = "NM",
				Quantity = 1,
			};

			// Act
			var isAddCardSuccess = await _userRepository.AddUserCard(user, cardOwned);
			var cardsOwned = await _userRepository.GetCardsOwned(user.Username);

			// Assert
			Assert.IsTrue(isAddCardSuccess);
			Assert.AreEqual(1, cardsOwned.Count);
		}
	}
}