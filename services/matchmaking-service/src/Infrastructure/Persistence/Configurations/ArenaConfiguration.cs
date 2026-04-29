using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OnlineGame.MatchmakingService.Core.Domain.Entities;

namespace OnlineGame.MatchmakingService.Infrastructure.Persistence.Configurations;

public sealed class ArenaConfiguration : IEntityTypeConfiguration<Arena>
{
    public void Configure(EntityTypeBuilder<Arena> builder)
    {
        builder.ToTable("Arenas");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.MinPower).IsRequired();
        builder.Property(x => x.MaxPower).IsRequired(false);
    }
}
