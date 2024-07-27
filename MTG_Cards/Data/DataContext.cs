using Microsoft.EntityFrameworkCore;
using MTG_Cards.Models;

namespace MTG_Cards.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
            
        }

        public virtual DbSet<Card> Cards { get; set; }
        public virtual DbSet<Edition> Editions { get; set; }
        public virtual DbSet<CardCondition> CardConditions { get; set; }
        public virtual DbSet<CardOwned> CardsOwned { get; set; }
        public virtual DbSet<User> Users { get; set; }
		public virtual DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
			modelBuilder.Entity<Edition>(entity =>
			{
				entity.HasIndex(e => e.Name).IsUnique();
			});

			modelBuilder.Entity<Card>()
				.HasIndex(c => c.Rarity)
				.HasDatabaseName("IX_Cards_Rarity");

			modelBuilder.Entity<Card>()
				.HasIndex(c => c.NMPrice)
				.HasDatabaseName("IX_Cards_NMPrice");

			modelBuilder.Entity<CardCondition>()
				.Property(c => c.Condition)
				.HasConversion<string>();
		}
    }
}
