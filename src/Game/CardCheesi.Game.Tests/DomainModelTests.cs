using CardCheesi.Game.Abstractions.DomainModels;
using CardCheesi.Game.DomainModels;

namespace CardCheesi.Game.Tests;

public sealed class GameStateTests
{
    private static readonly Card AnyCard = new(CardSuit.Hearts, CardRank.Ace);
    private const string TestCode = "TESTCD";

    [Fact]
    public void PlayCard_ThrowsNotImplementedException()
    {
        var state = GameFactory.CreateWaiting("Alice", TestCode);

        Assert.Throws<NotImplementedException>(() => state.PlayCard(Guid.NewGuid(), AnyCard));
    }

    [Fact]
    public void MakeMove_ThrowsNotImplementedException()
    {
        var state = GameFactory.CreateWaiting("Alice", TestCode);

        Assert.Throws<NotImplementedException>(() => state.MakeMove(Guid.NewGuid(), 1));
    }

    [Fact]
    public void IGameState_PlayCard_ThrowsNotImplementedException()
    {
        IGameState state = GameFactory.CreateWaiting("Alice", TestCode);

        Assert.Throws<NotImplementedException>(() => state.PlayCard(Guid.NewGuid(), AnyCard));
    }

    [Fact]
    public void IGameState_MakeMove_ThrowsNotImplementedException()
    {
        IGameState state = GameFactory.CreateWaiting("Alice", TestCode);

        Assert.Throws<NotImplementedException>(() => state.MakeMove(Guid.NewGuid(), 1));
    }

    [Fact]
    public void IGameState_AddPlayer_AddsPlayerViaInterface()
    {
        IGameState state = GameFactory.CreateWaiting("Alice", TestCode);
        var newPlayer = new Player(Guid.NewGuid(), "Bob", []);

        var updated = state.AddPlayer(newPlayer);

        Assert.Equal(2, updated.Players.Count);
        Assert.Contains(updated.Players, p => p.Name == "Bob");
    }

    [Fact]
    public void IGameState_Teams_ReturnsTeamList()
    {
        var state = GameFactory.Create(["Alice", "Bob", "Carol", "Dave"], TestCode);
        IGameState iState = state;

        Assert.Equal(state.Teams.Count, iState.Teams.Count);
        Assert.Equal(state.Teams[0].Id, iState.Teams[0].Id);
    }

    [Fact]
    public void IGameState_Players_ReturnsPlayerList()
    {
        IGameState state = GameFactory.CreateWaiting("Alice", TestCode);

        Assert.Single(state.Players);
        Assert.Equal("Alice", state.Players[0].Name);
    }

    [Fact]
    public void IGameState_Turn_ReturnsTurnState()
    {
        var state = GameFactory.Create(["Alice", "Bob", "Carol", "Dave"], TestCode);
        IGameState iState = state;

        Assert.NotNull(iState.Turn);
        Assert.Equal(state.Turn!.ActivePlayerId, iState.Turn.ActivePlayerId);
    }

    [Fact]
    public void IGameState_Turn_ReturnsNullForWaitingState()
    {
        IGameState state = GameFactory.CreateWaiting("Alice", TestCode);

        Assert.Null(state.Turn);
    }

    [Fact]
    public void IGameState_Deck_ReturnsDeck()
    {
        var state = GameFactory.Create(["Alice", "Bob", "Carol", "Dave"], TestCode);
        IGameState iState = state;

        Assert.NotNull(iState.Deck);
        Assert.Equal(state.Deck!.Cards.Count, iState.Deck.Cards.Count);
    }

    [Fact]
    public void IGameState_Deck_ReturnsNullForWaitingState()
    {
        IGameState state = GameFactory.CreateWaiting("Alice", TestCode);

        Assert.Null(state.Deck);
    }

    [Fact]
    public void IGameState_Hands_ReturnsHands()
    {
        var state = GameFactory.Create(["Alice", "Bob", "Carol", "Dave"], TestCode);
        IGameState iState = state;

        Assert.NotNull(iState.Hands);
        Assert.Equal(state.Hands!.Count, iState.Hands.Count);
    }

    [Fact]
    public void IGameState_Hands_ReturnsNullForWaitingState()
    {
        IGameState state = GameFactory.CreateWaiting("Alice", TestCode);

        Assert.Null(state.Hands);
    }
}

public class TeamTests
{
    [Fact]
    public void ITeam_Players_ReturnsMappedPlayerList()
    {
        var player1 = new Player(Guid.NewGuid(), "Alice", []);
        var player2 = new Player(Guid.NewGuid(), "Bob", []);
        var team = new Team(Guid.NewGuid(), [player1, player2]);

        ITeam iTeam = team;

        Assert.Equal(2, iTeam.Players.Count);
        Assert.Equal(player1.Id, iTeam.Players[0].Id);
        Assert.Equal(player2.Id, iTeam.Players[1].Id);
    }
}

public class PlayerTests
{
    [Fact]
    public void IPlayer_Pawns_ReturnsMappedPawnList()
    {
        var ownerId = Guid.NewGuid();
        var pawn = new Pawn(Guid.NewGuid(), ownerId, PawnStatus.Reserve, new ReserveLocation());
        var player = new Player(ownerId, "Alice", [pawn]);

        IPlayer iPlayer = player;

        Assert.Single(iPlayer.Pawns);
        Assert.Equal(pawn.Id, iPlayer.Pawns[0].Id);
    }
}
