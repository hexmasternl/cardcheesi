using CardCheesi.Game.Abstractions;
using CardCheesi.Game.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CardCheesi.Game.Tests;

public class GameRepositoryTests
{
    private static AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static GameState MakeWaitingGame(string code = "ABC123") =>
        GameFactory.CreateWaiting("Alice", code);

    [Fact]
    public async Task SaveAsync_NewGame_InsertsRow()
    {
        await using var db = CreateInMemoryContext();
        var repo = new GameRepository(db);
        var game = MakeWaitingGame();

        await repo.SaveAsync(game);

        var entity = await db.Games.FindAsync(game.Id);
        Assert.NotNull(entity);
        Assert.Equal(game.GameCode, entity.GameCode);
    }

    [Fact]
    public async Task SaveAsync_ExistingGame_UpdatesRow()
    {
        await using var db = CreateInMemoryContext();
        var repo = new GameRepository(db);
        var game = MakeWaitingGame();

        await repo.SaveAsync(game);

        var updated = game with { Status = GameStatus.InProgress };
        await repo.SaveAsync(updated);

        var entity = await db.Games.FindAsync(game.Id);
        Assert.NotNull(entity);
        Assert.Equal(GameStatus.InProgress, entity.State.Status);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingGame_ReturnsState()
    {
        await using var db = CreateInMemoryContext();
        var repo = new GameRepository(db);
        var game = MakeWaitingGame();
        await repo.SaveAsync(game);

        var result = await repo.GetByIdAsync(game.Id);

        Assert.NotNull(result);
        Assert.Equal(game.Id, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_MissingGame_ReturnsNull()
    {
        await using var db = CreateInMemoryContext();
        var repo = new GameRepository(db);

        var result = await repo.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByCodeAsync_ExistingGame_ReturnsState()
    {
        await using var db = CreateInMemoryContext();
        var repo = new GameRepository(db);
        var game = MakeWaitingGame("XYZ789");
        await repo.SaveAsync(game);

        var result = await repo.GetByCodeAsync("XYZ789");

        Assert.NotNull(result);
        Assert.Equal("XYZ789", result.GameCode);
    }

    [Fact]
    public async Task GetByCodeAsync_MissingCode_ReturnsNull()
    {
        await using var db = CreateInMemoryContext();
        var repo = new GameRepository(db);

        var result = await repo.GetByCodeAsync("NOCODE");

        Assert.Null(result);
    }
}
