using System.Security.Claims;
using System.Text;
using CardCheesi.Game;
using CardCheesi.Game.Abstractions.DomainModels;
using CardCheesi.Game.Api;
using CardCheesi.Game.Api.Auth;
using CardCheesi.Game.Api.Endpoints.Players;
using CardCheesi.Game.DomainModels;
using CardCheesi.Game.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Skip Npgsql registration in test environments to avoid real DB connections
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.AddNpgsqlDbContext<AppDbContext>("gamedb");
}

builder.Services.AddScoped<IGameRepository, GameRepository>();
builder.Services.AddHostedService<DatabaseMigrationWorker>();
builder.Services.AddHostedService<PlayerCleanupService>();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

// JWT configuration
builder.Services.AddOptions<JwtSettings>()
    .BindConfiguration(JwtSettings.SectionName)
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<JwtSettings>, JwtSettingsValidator>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
                          ?? new JwtSettings();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SigningKey)),
            ClockSkew = TimeSpan.Zero,
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(new
        {
            type = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
            title = "An unexpected error occurred.",
            status = 500,
        });
    });
});

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapRegisterPlayer();
app.MapRefreshToken();

app.MapPost("/games", async (CreateGameRequest request, HttpContext httpContext, IGameRepository repo, CancellationToken ct) =>
{
    var playerId = Guid.Parse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                              ?? httpContext.User.FindFirstValue("sub")!);
    var playerName = httpContext.User.FindFirstValue(ClaimTypes.Name)
                     ?? httpContext.User.FindFirstValue("name")!;

    const int maxAttempts = 5;
    for (var attempt = 0; attempt < maxAttempts; attempt++)
    {
        var code = GenerateGameCode();
        var existing = await repo.GetByCodeAsync(code, ct);
        if (existing is not null) continue;

        var game = GameFactory.CreateWaiting(playerName, code, playerId);
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
}).WithName("CreateGame")
  .RequireAuthorization()
  .Produces(StatusCodes.Status200OK)
  .ProducesProblem(StatusCodes.Status401Unauthorized)
  .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
  .WithOpenApi();

app.MapGet("/games/{code}", async (string code, IGameRepository repo, CancellationToken ct) =>
{
    var game = await repo.GetByCodeAsync(code, ct);
    return game is null ? Results.NotFound() : Results.Ok(game);
}).WithName("GetGame");

app.MapPost("/games/{code}/join", async (string code, JoinGameRequest request, HttpContext httpContext, IGameRepository repo, CancellationToken ct) =>
{
    var game = await repo.GetByCodeAsync(code, ct);
    if (game is null)
        return Results.NotFound(new { error = $"Game with code '{code}' not found." });

    if (game.Status != GameStatus.Waiting)
        return Results.Conflict(new { error = "Game is not accepting new players." });

    if (game.Players.Count >= 4)
        return Results.Conflict(new { error = "Game is already full." });

    var playerId = Guid.Parse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                              ?? httpContext.User.FindFirstValue("sub")!);

    if (game.Players.Any(p => p.Id == playerId))
        return Results.Conflict(new { error = "You have already joined this game." });

    var playerName = httpContext.User.FindFirstValue(ClaimTypes.Name)
                     ?? httpContext.User.FindFirstValue("name")!;

    var newPlayer = GameFactory.CreatePlayer(playerId, playerName);
    var updatedGame = game.AddPlayer(newPlayer);

    await repo.SaveAsync(updatedGame, ct);
    return Results.Ok(new { gameId = updatedGame.Id, playerId, gameCode = updatedGame.GameCode });
}).WithName("JoinGame")
  .RequireAuthorization()
  .Produces(StatusCodes.Status200OK)
  .ProducesProblem(StatusCodes.Status401Unauthorized)
  .ProducesProblem(StatusCodes.Status404NotFound)
  .ProducesProblem(StatusCodes.Status409Conflict)
  .WithOpenApi();

app.Run();

static string GenerateGameCode()
{
    const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    return new string(Enumerable.Range(0, 6).Select(_ => chars[Random.Shared.Next(chars.Length)]).ToArray());
}

record CreateGameRequest();
record JoinGameRequest();

public partial class Program { }

