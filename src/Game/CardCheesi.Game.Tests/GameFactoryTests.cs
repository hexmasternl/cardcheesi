using CardCheesi.Game.Abstractions.DomainModels;
using CardCheesi.Game.DomainModels;

namespace CardCheesi.Game.Tests;

public class GameFactoryTests
{
    private static readonly IReadOnlyList<string> FourNames = ["Alice", "Bob", "Carol", "Dave"];
    private const string TestCode = "ABCDEF";

    [Fact]
    public void Create_WithFourPlayerNames_ReturnsGameState()
    {
        var state = GameFactory.Create(FourNames, TestCode);

        Assert.NotNull(state);
        Assert.NotEqual(Guid.Empty, state.Id);
        Assert.Equal(TestCode, state.GameCode);
        Assert.Equal(GameStatus.InProgress, state.Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void Create_WithWrongPlayerCount_ThrowsArgumentException(int count)
    {
        var names = Enumerable.Range(0, count).Select(i => $"Player{i}").ToList();

        Assert.Throws<ArgumentException>(() => GameFactory.Create(names, TestCode));
    }

    [Fact]
    public void Create_AllPawnsStartInReserve()
    {
        var state = GameFactory.Create(FourNames, TestCode);

        foreach (var player in state.Players)
        foreach (var pawn in player.Pawns)
        {
            Assert.Equal(PawnStatus.Reserve, pawn.Status);
            Assert.IsType<ReserveLocation>(pawn.Location);
        }
    }

    [Fact]
    public void Create_EachPlayerHasFourPawns()
    {
        var state = GameFactory.Create(FourNames, TestCode);

        foreach (var player in state.Players)
            Assert.Equal(4, player.Pawns.Count);
    }

    [Fact]
    public void Create_PawnOwnerIdMatchesPlayer()
    {
        var state = GameFactory.Create(FourNames, TestCode);

        foreach (var player in state.Players)
        foreach (var pawn in player.Pawns)
            Assert.Equal(player.Id, pawn.OwnerId);
    }

    [Fact]
    public void Create_TwoTeamsWithTwoPlayersEach()
    {
        var state = GameFactory.Create(FourNames, TestCode);

        Assert.Equal(2, state.Teams.Count);
        Assert.All(state.Teams, t => Assert.Equal(2, t.Players.Count));
    }

    [Fact]
    public void Create_TeamAContainsPlayer0AndPlayer2()
    {
        var state = GameFactory.Create(FourNames, TestCode);

        var teamA = state.Teams[0];
        Assert.Equal(state.Players[0].Id, teamA.Players[0].Id);
        Assert.Equal(state.Players[2].Id, teamA.Players[1].Id);
    }

    [Fact]
    public void Create_TeamBContainsPlayer1AndPlayer3()
    {
        var state = GameFactory.Create(FourNames, TestCode);

        var teamB = state.Teams[1];
        Assert.Equal(state.Players[1].Id, teamB.Players[0].Id);
        Assert.Equal(state.Players[3].Id, teamB.Players[1].Id);
    }

    [Fact]
    public void Create_InitialTurnIsRound1WithFirstPlayerActive()
    {
        var state = GameFactory.Create(FourNames, TestCode);

        Assert.NotNull(state.Turn);
        Assert.Equal(1, state.Turn.RoundNumber);
        Assert.Equal(state.Players[0].Id, state.Turn.ActivePlayerId);
        Assert.Equal(state.Players[0].Id, state.Turn.DealerId);
    }

    [Fact]
    public void Create_DeckContains52Cards()
    {
        var state = GameFactory.Create(FourNames, TestCode);

        Assert.NotNull(state.Deck);
        Assert.Equal(52, state.Deck.Cards.Count);
    }

    [Fact]
    public void Create_AllHandsAreEmpty()
    {
        var state = GameFactory.Create(FourNames, TestCode);

        Assert.NotNull(state.Hands);
        Assert.Equal(4, state.Hands.Count);
        Assert.All(state.Hands, h => Assert.Empty(h.Cards));
    }

    [Fact]
    public void CreateWaiting_ReturnsWaitingStateWithOnePlayer()
    {
        var state = GameFactory.CreateWaiting("Alice", TestCode);

        Assert.Equal(GameStatus.Waiting, state.Status);
        Assert.Equal(TestCode, state.GameCode);
        Assert.Single(state.Players);
        Assert.Equal("Alice", state.Players[0].Name);
        Assert.Null(state.Turn);
        Assert.Null(state.Deck);
        Assert.Null(state.Hands);
    }
}
