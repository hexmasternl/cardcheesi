using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CardCheesi.Game.Abstractions.DomainModels;
using CardCheesi.Game.Api.Auth;
using CardCheesi.Game.DomainModels;
using CardCheesi.Game.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

    /// <summary>Registers a player and returns a Bearer token.</summary>
    private async Task<string> RegisterAndGetTokenAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/players", new { name });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return body.GetProperty("token").GetString()!;
    }

    [Fact]
    public async Task GetGame_ExistingCode_ReturnsGameState()
    {
        var token = await RegisterAndGetTokenAsync("Eve");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await _client.PostAsJsonAsync("/games", new { });
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var gameCode = createBody.GetProperty("gameCode").GetString()!;

        var response = await _client.GetAsync($"/games/{gameCode}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var state = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        Assert.Equal(gameCode, state.GetProperty("gameCode").GetString());
        Assert.Equal((int)GameStatus.Waiting, state.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task GetGame_NonExistentCode_Returns404()
    {
        var response = await _client.GetAsync("/games/ZZZZZZ");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostGames_CreatesGameWithCorrectCodeAndState()
    {
        var token = await RegisterAndGetTokenAsync("Alice");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/games", new { });

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
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsJsonAsync("/games", new { });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostJoin_AddsPlayerToExistingGame()
    {
        var creatorToken = await RegisterAndGetTokenAsync("Bob");
        var joinerToken = await RegisterAndGetTokenAsync("Carol");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", creatorToken);
        var createResponse = await _client.PostAsJsonAsync("/games", new { });
        var createBody = await createResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var gameCode = createBody.GetProperty("gameCode").GetString()!;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", joinerToken);
        var joinResponse = await _client.PostAsJsonAsync($"/games/{gameCode}/join", new { });

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
        var token = await RegisterAndGetTokenAsync("Dave");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/games/XXXXXX/join", new { });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostJoin_WithoutToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsJsonAsync("/games/AABBCC/join", new { });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    public class Factory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
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
