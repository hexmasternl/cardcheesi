using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CardCheesi.Game.Abstractions;
using CardCheesi.Game.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CardCheesi.Game.Tests;

public class GameApiIntegrationTests : IClassFixture<GameApiIntegrationTests.Factory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly Factory _factory;
    private readonly HttpClient _client;

    public GameApiIntegrationTests(Factory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostGames_CreatesGameWithCorrectCodeAndState()
    {
        var response = await _client.PostAsJsonAsync("/games", new { playerName = "Alice" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var gameCode = body.GetProperty("gameCode").GetString()!;
        var gameId = Guid.Parse(body.GetProperty("gameId").GetString()!);

        Assert.Equal(6, gameCode.Length);
        Assert.NotEqual(Guid.Empty, gameId);

        using var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGameRepository>();
        var saved = await repo.GetByCodeAsync(gameCode);

        Assert.NotNull(saved);
        Assert.Equal(gameId, saved.Id);
        Assert.Equal(gameCode, saved.GameCode);
        Assert.Equal(GameStatus.Waiting, saved.Status);
        Assert.Single(saved.Players);
        Assert.Equal("Alice", saved.Players[0].Name);
    }

    [Fact]
    public async Task PostJoin_AddsPlayerToExistingGame()
    {
        // Create a game first
        var createResponse = await _client.PostAsJsonAsync("/games", new { playerName = "Bob" });
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var gameCode = createBody.GetProperty("gameCode").GetString()!;

        // Join the game
        var joinResponse = await _client.PostAsJsonAsync($"/games/{gameCode}/join", new { playerName = "Carol" });

        Assert.Equal(HttpStatusCode.OK, joinResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IGameRepository>();
        var saved = await repo.GetByCodeAsync(gameCode);

        Assert.NotNull(saved);
        Assert.Equal(2, saved.Players.Count);
        Assert.Contains(saved.Players, p => p.Name == "Bob");
        Assert.Contains(saved.Players, p => p.Name == "Carol");
    }

    [Fact]
    public async Task PostJoin_NonExistentCode_Returns404()
    {
        var response = await _client.PostAsJsonAsync("/games/XXXXXX/join", new { playerName = "Dave" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    public class Factory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove all EF Core registrations for AppDbContext (including the pool added by AddNpgsqlDbContext)
                var descriptorsToRemove = services
                    .Where(d => d.ServiceType.FullName?.Contains("AppDbContext") == true
                             || d.ServiceType.FullName?.Contains("DbContextPool") == true
                             || (d.ServiceType.IsGenericType &&
                                 d.ServiceType.GenericTypeArguments.Any(t => t == typeof(AppDbContext))))
                    .ToList();

                foreach (var d in descriptorsToRemove)
                    services.Remove(d);

                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase("integration-tests"));
            });
        }
    }
}
