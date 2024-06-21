using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using MTG_Cards.Controllers;
using MTG_Cards.Data;
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

		[TestInitialize]
		public void Setup()
		{
			// Mock DbContext and DbSet
			var options = new DbContextOptionsBuilder<DataContext>()
				.UseInMemoryDatabase(databaseName: "TestDatabase")
				.Options;

			_context = new DataContext(options);
			_mockUserSet = new Mock<DbSet<User>>();

			

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
		}

		[TestMethod()]
		public void GetUserByUsername()
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
	}
}