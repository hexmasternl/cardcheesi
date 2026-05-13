using CardCheesi.Game.Abstractions.DomainModels;
using CardCheesi.Game.DomainModels;

namespace CardCheesi.Game.Tests;

public sealed class TurnStateTests
{
    [Fact]
    public void Round1_CardsThisRound_Is5()
    {
        var turn = new TurnState(Guid.NewGuid(), Guid.NewGuid(), RoundNumber: 1);

        Assert.Equal(5, turn.CardsThisRound);
    }

    [Fact]
    public void Round2_CardsThisRound_Is4()
    {
        var turn = new TurnState(Guid.NewGuid(), Guid.NewGuid(), RoundNumber: 2);

        Assert.Equal(4, turn.CardsThisRound);
    }

    [Fact]
    public void Round3_CardsThisRound_Is4()
    {
        var turn = new TurnState(Guid.NewGuid(), Guid.NewGuid(), RoundNumber: 3);

        Assert.Equal(4, turn.CardsThisRound);
    }

    [Fact]
    public void TurnState_IsImmutable_WhenUpdatingRound()
    {
        var original = new TurnState(Guid.NewGuid(), Guid.NewGuid(), RoundNumber: 1);
        var next = original with { RoundNumber = 2 };

        Assert.Equal(1, original.RoundNumber);
        Assert.Equal(2, next.RoundNumber);
    }
}
