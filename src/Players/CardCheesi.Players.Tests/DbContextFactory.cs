using CardCheesi.Players.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CardCheesi.Players.Tests;

internal static class DbContextFactory
{
    public static PlayersDbContext CreateInMemory(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<PlayersDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;
        return new PlayersDbContext(options);
    }
}
