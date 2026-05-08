namespace CardCheesi.Game.Abstractions;

/// <summary>Abstraction over random number generation for testability.</summary>
public interface IRandom
{
    /// <returns>A random integer in [minValue, maxValue).</returns>
    int Next(int minValue, int maxValue);
}
