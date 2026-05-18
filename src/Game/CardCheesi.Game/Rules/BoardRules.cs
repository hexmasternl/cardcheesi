namespace CardCheesi.Game.Rules;

internal static class BoardRules
{
    private static readonly int[] HomePositions = [1, 17, 33, 49];

    public static int HomePosition(int playerIndex) => HomePositions[playerIndex];

    /// <summary>
    /// Finish entry threshold: the last board position before entering the finish corridor.
    /// P1=64, P2=16, P3=32, P4=48.
    /// </summary>
    public static int FinishEntryThreshold(int playerIndex)
        => ((HomePositions[playerIndex] - 2 + 64) % 64) + 1;

    /// <summary>
    /// Steps traveled from home along the board loop.
    /// 0 = at home position, 63 = at finish entry threshold.
    /// </summary>
    public static int PathDistance(int position, int homePosition)
        => (position - homePosition + 64) % 64;

    /// <summary>Board position after moving <paramref name="steps"/> forward (1-based, wraps 1-64).</summary>
    public static int AdvanceBoardPosition(int position, int steps)
        => (position - 1 + steps) % 64 + 1;

    /// <summary>Board position after moving <paramref name="steps"/> backward (1-based, wraps 1-64).</summary>
    public static int RetreatBoardPosition(int position, int steps)
        => ((position - 1 - steps) % 64 + 64) % 64 + 1;

    /// <summary>
    /// All board positions traversed when moving forward <paramref name="count"/> steps from
    /// <paramref name="from"/>, NOT including <paramref name="from"/> itself.
    /// </summary>
    public static IEnumerable<int> ForwardPath(int from, int count)
    {
        for (int i = 1; i <= count; i++)
            yield return (from - 1 + i) % 64 + 1;
    }

    /// <summary>
    /// All board positions traversed when moving backward <paramref name="count"/> steps from
    /// <paramref name="from"/>, NOT including <paramref name="from"/> itself.
    /// </summary>
    public static IEnumerable<int> BackwardPath(int from, int count)
    {
        for (int i = 1; i <= count; i++)
            yield return ((from - 1 - i) % 64 + 64) % 64 + 1;
    }
}
