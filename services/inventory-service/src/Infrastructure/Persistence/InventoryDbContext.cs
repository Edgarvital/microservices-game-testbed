using OnlineGame.InventoryService.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace OnlineGame.InventoryService.Infrastructure.Persistence;

public class InventoryDbContext : DbContext
{
    public DbSet<Card> Cards => Set<Card>();
    public DbSet<PlayerInventory> PlayerInventories => Set<PlayerInventory>();
    public DbSet<PlayerCard> PlayerCards => Set<PlayerCard>();
    public DbSet<PlayerDeck> PlayerDecks => Set<PlayerDeck>();
    public DbSet<PlayerDeckSlot> PlayerDeckSlots => Set<PlayerDeckSlot>();

    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Card
        modelBuilder.Entity<Card>().HasKey(c => c.Id);
        modelBuilder.Entity<Card>().Property(c => c.Name).IsRequired().HasMaxLength(100);
        modelBuilder.Entity<Card>().Property(c => c.Description).IsRequired().HasMaxLength(500);
        modelBuilder.Entity<Card>().Property(c => c.BaseDamage).IsRequired();
        modelBuilder.Entity<Card>().Property(c => c.Rarity).IsRequired();

        // PlayerInventory
        modelBuilder.Entity<PlayerInventory>().HasKey(pi => pi.PlayerId);
        modelBuilder.Entity<PlayerInventory>().Property(pi => pi.BaseLevel).IsRequired();
        modelBuilder.Entity<PlayerInventory>().Property(pi => pi.BaseHp).IsRequired();
        modelBuilder.Entity<PlayerInventory>().Property(pi => pi.BaseAttack).IsRequired();
        modelBuilder.Entity<PlayerInventory>().Property(pi => pi.LastUpdate).IsRequired();
        modelBuilder.Entity<PlayerInventory>()
            .HasMany(pi => pi.Cards)
            .WithOne()
            .HasForeignKey(pc => pc.PlayerId);
        modelBuilder.Entity<PlayerInventory>()
            .HasMany(pi => pi.Decks)
            .WithOne()
            .HasForeignKey(pd => pd.PlayerId);

        // PlayerCard
        modelBuilder.Entity<PlayerCard>().HasKey(pc => new { pc.PlayerId, pc.CardId });
        modelBuilder.Entity<PlayerCard>().Property(pc => pc.CurrentLevel).IsRequired();
        modelBuilder.Entity<PlayerCard>().Property(pc => pc.CountDuplicates).IsRequired();
        modelBuilder.Entity<PlayerCard>().Property(pc => pc.DateObtained).IsRequired();
        modelBuilder.Entity<PlayerCard>().Property(pc => pc.LastUpgrade);
        modelBuilder.Entity<PlayerCard>()
            .HasOne(pc => pc.Card)
            .WithMany()
            .HasForeignKey(pc => pc.CardId);

        // PlayerDeck
        modelBuilder.Entity<PlayerDeck>().HasKey(pd => pd.Id);
        modelBuilder.Entity<PlayerDeck>().Property(pd => pd.Name).IsRequired().HasMaxLength(100);
        modelBuilder.Entity<PlayerDeck>()
            .HasMany(pd => pd.Slots)
            .WithOne(s => s.Deck)
            .HasForeignKey(s => s.DeckId);

        // PlayerDeckSlot
        modelBuilder.Entity<PlayerDeckSlot>().HasKey(s => new { s.DeckId, s.SlotIndex });
        modelBuilder.Entity<PlayerDeckSlot>()
            .HasOne(s => s.Card)
            .WithMany()
            .HasForeignKey(s => s.CardId);
    }
}

