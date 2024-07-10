using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using MTG_Cards.Data;
using MTG_Cards.DTOs;
using MTG_Cards.Models;
using MTG_Cards.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MTG_CardsTests.Repositories
{
	[TestClass()]
	public class EditionRepositoryTests
	{
		private DataContext? _context;
		private Mock<IDistributedCache>? _mockCache;
		private EditionRepository? _editionRepository;
		private Mock<DbSet<Edition>>? _mockEditionSet;

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

		private void SetupMockDbSet<T>(Mock<DbSet<T>> mockSet, IQueryable<T> data) where T : class
		{
			mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(data.Provider);
			mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
			mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
			mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
		}

		private void SeedDatabase()
		{
			var cardCondition = new CardCondition { Id = 1, CardId = 1, Condition = Condition.NM, Quantity = 1 };
			var card = new Card
			{
				Id = 1,
				Name = $"Card 1",
				ImageURL = "Image URL",
				Conditions = [cardCondition],
				NMPrice = 4,
				Rarity = Rarity.Rare,
				IsFoil = false
			};

			var editions = new List<Edition>()
			{
				new Edition { Id = 1, Name = "Edition Name", Code = "edition-code", Cards = [card] }
			};
			SetupMockDbSet(_mockEditionSet!, editions.AsQueryable());

			// Populate DB
			_context!.Editions.AddRange(editions);
			_context.SaveChanges();
		}

		[TestInitialize]
		public void Setup()
		{
			MockDbContext();
			_mockEditionSet = new Mock<DbSet<Edition>>();
			ClearDatabase();
			SeedDatabase();

			// Mock cache
			_mockCache = new Mock<IDistributedCache>();

			// Repository instance
			_editionRepository = new EditionRepository(_context!, _mockCache.Object);
		}

		[TestMethod()]
		public async Task TestGetEditionsNames()
		{
			// Act
			var editions = await _editionRepository!.GetEditionNames();

			// Assert
			Assert.IsTrue(editions.Count == 1);
			Assert.AreEqual("Edition Name", editions[0].Name);
			Assert.AreEqual("edition-code", editions[0].Code);
		}

		[TestMethod()]
		public async Task GetEditionById_InvalidId()
		{
			// Act
			var edition = await _editionRepository!.GetEditionById(id: 2);

			// Assert
			Assert.IsNull(edition);
		}

		[TestMethod()]
		public async Task GetEditionById_ValidId()
		{
			// Act
			EditionDTO? edition = await _editionRepository!.GetEditionById(id: 1);

			// Assert
			Assert.IsNotNull(edition);
			Assert.AreEqual("Edition Name", edition?.Name);
			Assert.IsTrue(edition?.Cards.Count == 1);
		}

		[TestMethod()]
		public async Task GetEditionsDropdown_ReturnsValid()
		{
			// Act
			List<EditionDropdownDTO> dropdown = await _editionRepository!.GetEditionsDropdown();

			// Assert
			Assert.AreEqual("Edition Name", dropdown[0].Name);
			Assert.AreEqual(1, dropdown[0].Value);
		}
	}
}
