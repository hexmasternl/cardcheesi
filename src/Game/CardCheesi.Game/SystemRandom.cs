using CardCheesi.Game.Abstractions.DomainModels;

namespace CardCheesi.Game;

/// <summary>Production <see cref="IRandom"/> implementation backed by <see cref="Random.Shared"/>.</summary>
public sealed class SystemRandom : IRandom
{
    public static readonly SystemRandom Instance = new();

    private SystemRandom() { }

    public int Next(int minValue, int maxValue) => Random.Shared.Next(minValue, maxValue);
}
