using Microsoft.EntityFrameworkCore;
using OnlineGame.MatchmakingService.Core.Domain.Entities;
using OnlineGame.MatchmakingService.Infrastructure.Persistence;

namespace OnlineGame.InventoryService.Infrastructure.Persistence;

public static class ArenaSeeder
{
    public static async Task SeedAsync(MatchmakingDbContext dbContext, CancellationToken cancellationToken = default)
	{
		if (await dbContext.Arenas.AnyAsync(cancellationToken))
		{
			return;
		}

		dbContext.Arenas.AddRange(
			new Arena(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Arena Bronze", 0, 400),
			new Arena(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Arena Silver", 401, 800),
			new Arena(Guid.Parse("33333333-3333-3333-3333-333333333333"), "Arena Gold", 801, null));

		await dbContext.SaveChangesAsync(cancellationToken);
	}
}