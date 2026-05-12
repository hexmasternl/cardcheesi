using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CardCheesi.Game.Abstractions.DomainModels;
using CardCheesi.Game.Api.Auth;
using CardCheesi.Game.DomainModels;
using CardCheesi.Game.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CardCheesi.Game.Tests;

public class GameApiIntegrationTests : IClassFixture<GameApiIntegrationTests.Factory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly Factory _factory;

    public GameApiIntegrationTests(Factory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient() => _factory.CreateClient();

    /// <summary>Registers a player and returns a Bearer token.</summary>
    private async Task<string> RegisterAndGetTokenAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/players", new { name });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return body.GetProperty("token").GetString()!;
    }

    [Fact]
    public async Task GetGame_ExistingCode_ReturnsGameState()
    {
        var client = CreateClient();
        var token = await RegisterAndGetTokenAsync(client, "Eve");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await client.PostAsJsonAsync("/api/games", new { });
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var gameCode = createBody.GetProperty("gameCode").GetString()!;

        var response = await client.GetAsync($"/api/games/{gameCode}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var state = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(gameCode, state.GetProperty("gameCode").GetString());
        Assert.Equal((int)GameStatus.Waiting, state.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task GetGame_NonExistentCode_Returns404()
    {
        var client = CreateClient();
        var response = await client.GetAsync("/api/games/ZZZZZZ");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostGames_CreatesGameWithCorrectCodeAndState()
    {
        var client = CreateClient();
        var token = await RegisterAndGetTokenAsync(client, "Alice");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/games", new { });

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
    public async Task PostGames_WithoutToken_Returns401()
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/games", new { });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostJoin_AddsPlayerToExistingGame()
    {
        var creatorClient = CreateClient();
        var joinerClient = CreateClient();

        var creatorToken = await RegisterAndGetTokenAsync(creatorClient, "Bob");
        var joinerToken = await RegisterAndGetTokenAsync(joinerClient, "Carol");

        creatorClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", creatorToken);
        var createResponse = await creatorClient.PostAsJsonAsync("/api/games", new { });
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var gameCode = createBody.GetProperty("gameCode").GetString()!;

        joinerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", joinerToken);
        var joinResponse = await joinerClient.PostAsJsonAsync($"/api/games/{gameCode}/join", new { });

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
        var client = CreateClient();
        var token = await RegisterAndGetTokenAsync(client, "Dave");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsJsonAsync("/api/games/XXXXXX/join", new { });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostJoin_WithoutToken_Returns401()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/games/AABBCC/join", new { });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostJoin_AlreadyInGame_Returns409()
    {
        var client = CreateClient();
        var token = await RegisterAndGetTokenAsync(client, "Frank");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await client.PostAsJsonAsync("/api/games", new { });
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var gameCode = createBody.GetProperty("gameCode").GetString()!;

        // Try to join the game the player already created (i.e. is already in)
        var joinResponse = await client.PostAsJsonAsync($"/api/games/{gameCode}/join", new { });

        Assert.Equal(HttpStatusCode.Conflict, joinResponse.StatusCode);
    }

    [Fact]
    public async Task PostJoin_FullGame_Returns409()
    {
        // Create 5 distinct clients: 1 creator + 4 joiners (4th join should fail)
        var clients = Enumerable.Range(0, 5).Select(_ => CreateClient()).ToArray();
        var names = new[] { "Player1", "Player2", "Player3", "Player4", "Player5" };

        for (var i = 0; i < clients.Length; i++)
        {
            var t = await RegisterAndGetTokenAsync(clients[i], names[i]);
            clients[i].DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", t);
        }

        var createResponse = await clients[0].PostAsJsonAsync("/api/games", new { });
        var gameCode = (await createResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions))
            .GetProperty("gameCode").GetString()!;

        // Players 2, 3, 4 join successfully
        for (var i = 1; i <= 3; i++)
        {
            var r = await clients[i].PostAsJsonAsync($"/api/games/{gameCode}/join", new { });
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        }

        // Player 5 should be rejected — game is full
        var fullResponse = await clients[4].PostAsJsonAsync($"/api/games/{gameCode}/join", new { });
        Assert.Equal(HttpStatusCode.Conflict, fullResponse.StatusCode);
    }

    [Fact]
    public async Task GetGameEvents_UnknownCode_Returns404()
    {
        var client = CreateClient();
        var response = await client.GetAsync("/api/games/ZZZZZZ/events");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetGameEvents_KnownCode_ReturnsEventStream()
    {
        var client = CreateClient();
        var token = await RegisterAndGetTokenAsync(client, "StreamTester");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await client.PostAsJsonAsync("/api/games", new { });
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var gameCode = createBody.GetProperty("gameCode").GetString()!;

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        using var response = await client.GetAsync(
            $"/api/games/{gameCode}/events",
            HttpCompletionOption.ResponseHeadersRead,
            cts.Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);
    }

    public class Factory : WebApplicationFactory<Program>
    {
        private readonly InMemoryDatabaseRoot _dbRoot = new();

        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:SigningKey"] = "integration-test-signing-key-32bytes!",
                    ["Jwt:Issuer"] = "cardcheesi-api",
                    ["Jwt:Audience"] = "cardcheesi-api",
                    ["Jwt:CookieSecure"] = "false",
                });
            });

            builder.ConfigureServices(services =>
            {
                // Remove any existing AppDbContext registrations to ensure clean test isolation
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase("integration-tests", _dbRoot));
            });
        }
    }
}
