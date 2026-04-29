using Microsoft.EntityFrameworkCore;
using OnlineGame.MatchmakingService.Core.Domain.Entities;

namespace OnlineGame.MatchmakingService.Infrastructure.Persistence;

public sealed class MatchmakingDbContext : DbContext
{
	public DbSet<Arena> Arenas => Set<Arena>();

	public MatchmakingDbContext(DbContextOptions<MatchmakingDbContext> options) : base(options)
	{
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(MatchmakingDbContext).Assembly);
	}
}
