using Microsoft.EntityFrameworkCore;
using MTG_Cards.Models;

namespace MTG_Cards.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
            
        }

        public DbSet<Card> Cards { get; set; }
        public DbSet<Edition> Editions { get; set; }
        public DbSet<CardCondition> CardConditions { get; set; }
        public DbSet<CardOwned> CardsOwned { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Edition>(entity =>
            {
                entity.HasIndex(e => e.Name).IsUnique();
            });

            modelBuilder.Entity<CardCondition>()
                .Property(c => c.Condition)
                .HasConversion<string>();

			modelBuilder.Entity<Card>()
	            .Property(c => c.Rarity)
	            .HasConversion<string>();
		}
    }
}
