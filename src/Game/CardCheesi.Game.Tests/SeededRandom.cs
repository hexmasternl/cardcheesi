using CardCheesi.Game.Abstractions;

namespace CardCheesi.Game.Tests;

/// <summary>Seeded IRandom implementation for deterministic tests.</summary>
internal sealed class SeededRandom(int seed) : IRandom
{
    private readonly Random _random = new(seed);

    public int Next(int minValue, int maxValue) => _random.Next(minValue, maxValue);
}
