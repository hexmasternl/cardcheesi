using CardCheesi.Game.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CardCheesi.Players.Tests;

internal static class DbContextFactory
{
    public static AppDbContext CreateInMemory(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
