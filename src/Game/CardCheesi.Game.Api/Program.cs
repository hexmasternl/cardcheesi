using CardCheesi.Game;
using CardCheesi.Game.Abstractions;
using CardCheesi.Game.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<AppDbContext>("gamedb");
builder.Services.AddScoped<IGameRepository, GameRepository>();
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.IsRelational())
        await db.Database.MigrateAsync();
    else
        await db.Database.EnsureCreatedAsync();
}

app.UseHttpsRedirection();

app.MapPost("/games", async (CreateGameRequest request, IGameRepository repo, CancellationToken ct) =>
{
    const int maxAttempts = 5;
    for (var attempt = 0; attempt < maxAttempts; attempt++)
    {
        var code = GenerateGameCode();
        var existing = await repo.GetByCodeAsync(code, ct);
        if (existing is not null) continue;

        var game = GameFactory.CreateWaiting(request.PlayerName, code);
        try
        {
            await repo.SaveAsync(game, ct);
            return Results.Ok(new { gameId = game.Id, gameCode = game.GameCode });
        }
        catch (DbUpdateException)
        {
            // unique constraint race — retry
        }
    }

    return Results.Problem("Could not generate a unique game code. Please try again.", statusCode: 503);
}).WithName("CreateGame");

app.MapPost("/games/{code}/join", async (string code, JoinGameRequest request, IGameRepository repo, CancellationToken ct) =>
{
    var game = await repo.GetByCodeAsync(code, ct);
    if (game is null)
        return Results.NotFound(new { error = $"Game with code '{code}' not found." });

    var playerId = Guid.NewGuid();
    var newPlayer = new Player(
        Id: playerId,
        Name: request.PlayerName,
        Pawns: []);

    var updatedPlayers = game.Players.Append(newPlayer).ToList().AsReadOnly();
    var updatedGame = game with { Players = updatedPlayers };

    await repo.SaveAsync(updatedGame, ct);
    return Results.Ok(new { gameId = updatedGame.Id, playerId, gameCode = updatedGame.GameCode });
}).WithName("JoinGame");

app.Run();

static string GenerateGameCode()
{
    const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    return new string(Enumerable.Range(0, 6).Select(_ => chars[Random.Shared.Next(chars.Length)]).ToArray());
}

record CreateGameRequest(string PlayerName);
record JoinGameRequest(string PlayerName);

public partial class Program { }

