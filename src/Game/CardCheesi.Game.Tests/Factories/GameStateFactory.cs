using Bogus;
using CardCheesi.Game.Abstractions.DomainModels;
using CardCheesi.Game.DomainModels;

namespace CardCheesi.Game.Tests.Factories;

internal static class GameStateFactory
{
    private static readonly Faker _faker = new();

    public static GameState Create(
        Guid? id = null,
        string? gameCode = null,
        GameStatus? status = null,
        List<Player>? players = null,
        List<Team>? teams = null)
    {
        return new GameState(
            Id: id ?? Guid.NewGuid(),
            GameCode: gameCode ?? _faker.Random.AlphaNumeric(6).ToUpperInvariant(),
            Status: status ?? GameStatus.Waiting,
            Teams: teams ?? [],
            Players: players ?? [],
            Turn: null,
            Deck: null,
            Hands: null);
    }
}
