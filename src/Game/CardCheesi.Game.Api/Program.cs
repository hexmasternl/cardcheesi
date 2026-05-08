using CardCheesi.Game;
using CardCheesi.Game.Abstractions.DomainModels;
using CardCheesi.Game.Api;
using CardCheesi.Game.DomainModels;
using CardCheesi.Game.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<AppDbContext>("gamedb");
builder.Services.AddScoped<IGameRepository, GameRepository>();
builder.Services.AddHostedService<DatabaseMigrationWorker>();
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
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

app.MapGet("/games/{code}", async (string code, IGameRepository repo, CancellationToken ct) =>
{
    var game = await repo.GetByCodeAsync(code, ct);
    return game is null ? Results.NotFound() : Results.Ok(game);
}).WithName("GetGame");

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

    var updatedGame = game.AddPlayer(newPlayer);

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

