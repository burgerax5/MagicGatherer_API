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
		private UserController _userController;
		private Mock<HttpContext> _mockHttpContext;
		private Mock<HttpResponse> _mockHttpResponse;
		private Mock<IResponseCookies> _mockResponseCookies;

		[TestInitialize]
		public void Setup()
		{
			// Mock Http
			_mockHttpContext = new Mock<HttpContext>();
			_mockHttpResponse = new Mock<HttpResponse>();
			_mockResponseCookies = new Mock<IResponseCookies>();

			_mockHttpResponse.Setup(r => r.Cookies).Returns(_mockResponseCookies.Object);
			_mockHttpContext.Setup(c => c.Response).Returns(_mockHttpResponse.Object);

			// Mock DbContext and DbSet
			var options = new DbContextOptionsBuilder<DataContext>()
				.UseInMemoryDatabase(databaseName: "TestDatabase")
				.Options;

			_context = new DataContext(options);
			_mockUserSet = new Mock<DbSet<User>>();

			Environment.SetEnvironmentVariable("SECRET_KEY", "super-secret-key");

			var users = new List<User>()
			{
				new User { Id = 1, Username = "Bob", Password = "Bob's Password", Salt = "Salt" }
			}.AsQueryable();

			_mockUserSet.As<IQueryable<User>>().Setup(m => m.Provider).Returns(users.Provider);
			_mockUserSet.As<IQueryable<User>>().Setup(m => m.Expression).Returns(users.Expression);
			_mockUserSet.As<IQueryable<User>>().Setup(m => m.ElementType).Returns(users.ElementType);
			_mockUserSet.As<IQueryable<User>>().Setup(m => m.GetEnumerator()).Returns(users.GetEnumerator());

			_context.Users.AddRange(users);
			_context.SaveChanges();

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
			var expectedUser = new User { Id = 2, Username = username, Password = "Sam's Password" };

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
	}
}