using CardCheesi.Game.Rules;

namespace CardCheesi.Game.Tests;

public class BoardRulesTests
{
    // -----------------------------------------------------------------------
    // HomePosition
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 17)]
    [InlineData(2, 33)]
    [InlineData(3, 49)]
    public void HomePosition_ReturnsCorrectPosition(int playerIndex, int expected)
        => Assert.Equal(expected, BoardRules.HomePosition(playerIndex));

    // -----------------------------------------------------------------------
    // FinishEntryThreshold
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(0, 64)]
    [InlineData(1, 16)]
    [InlineData(2, 32)]
    [InlineData(3, 48)]
    public void FinishEntryThreshold_ReturnsCorrectThreshold(int playerIndex, int expected)
        => Assert.Equal(expected, BoardRules.FinishEntryThreshold(playerIndex));

    // -----------------------------------------------------------------------
    // PathDistance
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(1,  1,  0)]   // P1 at home → 0 steps from home
    [InlineData(5,  1,  4)]   // 4 steps past P1 home
    [InlineData(64, 1, 63)]   // at P1 finish threshold
    [InlineData(17, 17, 0)]   // P2 at home
    [InlineData(1,  17, 48)]  // P2 pawn at position 1 → 48 steps from P2 home
    [InlineData(33, 33, 0)]   // P3 at home
    [InlineData(49, 49, 0)]   // P4 at home
    public void PathDistance_ReturnsCorrectDistance(int position, int homePosition, int expected)
        => Assert.Equal(expected, BoardRules.PathDistance(position, homePosition));

    // -----------------------------------------------------------------------
    // AdvanceBoardPosition
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(1,  4,  5)]   // simple forward
    [InlineData(63, 3,  2)]   // wraps past 64
    [InlineData(64, 1,  1)]   // wraps from 64 to 1
    [InlineData(60, 5,  1)]   // wraps near end
    [InlineData(1,  64, 1)]   // full loop back to start
    public void AdvanceBoardPosition_ReturnsCorrectPosition(int pos, int steps, int expected)
        => Assert.Equal(expected, BoardRules.AdvanceBoardPosition(pos, steps));

    // -----------------------------------------------------------------------
    // RetreatBoardPosition
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(5,  4,  1)]   // simple backward
    [InlineData(3,  4, 63)]   // wraps below 1
    [InlineData(1,  1, 64)]   // wraps from 1 to 64
    [InlineData(4,  4, 64)]   // lands exactly on 64
    public void RetreatBoardPosition_ReturnsCorrectPosition(int pos, int steps, int expected)
        => Assert.Equal(expected, BoardRules.RetreatBoardPosition(pos, steps));

    // -----------------------------------------------------------------------
    // ForwardPath
    // -----------------------------------------------------------------------

    [Fact]
    public void ForwardPath_ReturnsIntermediatePositions()
    {
        var path = BoardRules.ForwardPath(1, 3).ToList();
        Assert.Equal([2, 3, 4], path);
    }

    [Fact]
    public void ForwardPath_WrapsAroundBoard()
    {
        var path = BoardRules.ForwardPath(63, 3).ToList();
        Assert.Equal([64, 1, 2], path);
    }

    [Fact]
    public void ForwardPath_ZeroSteps_ReturnsEmpty()
    {
        var path = BoardRules.ForwardPath(5, 0).ToList();
        Assert.Empty(path);
    }

    [Fact]
    public void ForwardPath_SingleStep_ReturnsSingleNextPosition()
    {
        var path = BoardRules.ForwardPath(10, 1).ToList();
        Assert.Equal([11], path);
    }

    // -----------------------------------------------------------------------
    // BackwardPath
    // -----------------------------------------------------------------------

    [Fact]
    public void BackwardPath_ReturnsIntermediatePositions()
    {
        var path = BoardRules.BackwardPath(5, 3).ToList();
        Assert.Equal([4, 3, 2], path);
    }

    [Fact]
    public void BackwardPath_WrapsAroundBoard()
    {
        var path = BoardRules.BackwardPath(2, 3).ToList();
        Assert.Equal([1, 64, 63], path);
    }

    [Fact]
    public void BackwardPath_ZeroSteps_ReturnsEmpty()
    {
        var path = BoardRules.BackwardPath(5, 0).ToList();
        Assert.Empty(path);
    }

    [Fact]
    public void BackwardPath_FromOne_WrapsTo64()
    {
        var path = BoardRules.BackwardPath(1, 1).ToList();
        Assert.Equal([64], path);
    }
}
