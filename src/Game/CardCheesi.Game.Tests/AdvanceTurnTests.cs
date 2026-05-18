using CardCheesi.Game.Abstractions.DomainModels;
using CardCheesi.Game.DomainModels;

namespace CardCheesi.Game.Tests;

public sealed class AdvanceTurnTests
{
    private const string Code = "TURN01";

    private static GameState CreateGame()
        => GameFactory.Create(["Alice", "Bob", "Carol", "Dave"], Code, new SeededRandom(123));

    [Fact]
    public void AdvanceTurn_WhenHandsRemain_AdvancesToNextPlayer()
    {
        var state = CreateGame();

        var result = state.AdvanceTurn();

        Assert.Equal(state.Players[1].Id, result.Turn!.ActivePlayerId);
        Assert.Equal(state.Turn!.DealerId, result.Turn.DealerId);
        Assert.Equal(state.Turn.RoundNumber, result.Turn.RoundNumber);
        Assert.Same(state.Hands, result.Hands);
    }

    [Fact]
    public void AdvanceTurn_WhenRoundHandsEmpty_DealsNextRoundOfFourCards()
    {
        var state = CreateGame();
        state = state with
        {
            Hands = state.Hands!
                .Select(h => new PlayerHand(h.PlayerId, Array.Empty<Card>()))
                .ToList()
                .AsReadOnly(),
        };

        var result = state.AdvanceTurn();

        Assert.Equal(2, result.Turn!.RoundNumber);
        Assert.Equal(state.Players[1].Id, result.Turn.ActivePlayerId);
        Assert.All(result.Hands!, hand => Assert.Equal(4, hand.Cards.Count));
        Assert.Equal(16, result.Hands!.Sum(hand => hand.Cards.Count));
    }

    [Fact]
    public void AdvanceTurn_WhenThirdRoundEnds_StartsNextDealerCycle()
    {
        var state = CreateGame();
        state = state with
        {
            Turn = new TurnState(state.Players[3].Id, state.Players[0].Id, 3),
            Hands = state.Hands!
                .Select(h => new PlayerHand(h.PlayerId, Array.Empty<Card>()))
                .ToList()
                .AsReadOnly(),
        };

        var result = state.AdvanceTurn(new SeededRandom(456));

        Assert.Equal(state.Players[2].Id, result.Turn!.ActivePlayerId);
        Assert.Equal(state.Players[1].Id, result.Turn.DealerId);
        Assert.Equal(1, result.Turn.RoundNumber);
        Assert.All(result.Hands!, hand => Assert.Equal(5, hand.Cards.Count));
        Assert.Equal(20, result.Hands!.Sum(hand => hand.Cards.Count));
    }
}
