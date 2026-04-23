using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineGame.AuthService.Core.Domain;

namespace OnlineGame.AuthService.Infrastructure.Persistence;

public sealed class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options)
        : base(options)
    {
    }

    public DbSet<Player> Players => Set<Player>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new PlayerConfiguration());
    }
}

internal sealed class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.ToTable("players");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.DeviceId).HasColumnName("device_id");
        builder.Property(x => x.Email).HasColumnName("email");
        builder.Property(x => x.PasswordHash).HasColumnName("password_hash");
        builder.Property(x => x.BaseLevel).HasColumnName("base_level").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.LastLoginAt).HasColumnName("last_login_at").IsRequired();

        builder.Ignore(x => x.IsGuest);

        builder.HasIndex(x => x.Email)
            .IsUnique()
            .HasFilter("email is not null")
            .HasDatabaseName("ux_players_email");

        builder.HasIndex(x => x.DeviceId)
            .IsUnique()
            .HasFilter("device_id is not null")
            .HasDatabaseName("ux_players_device_id");
    }
}