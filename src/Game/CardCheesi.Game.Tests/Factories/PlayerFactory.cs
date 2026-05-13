using Bogus;
using CardCheesi.Game.DomainModels;

namespace CardCheesi.Game.Tests.Factories;

internal static class PlayerFactory
{
    private static readonly Faker _faker = new();

    public static Player Create(Guid? id = null, string? name = null)
        => new(
            Id: id ?? Guid.NewGuid(),
            Name: name ?? _faker.Internet.UserName(),
            Pawns: []);
}
