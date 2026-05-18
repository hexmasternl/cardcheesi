using CardCheesi.Core;
using CardCheesi.Game.Abstractions;
using CardCheesi.Game.Abstractions.DomainModels;
using CardCheesi.Game.DomainModels;
using CardCheesi.Game.Features.DisposeHand;
using CardCheesi.Game.Tests.Factories;
using Moq;

namespace CardCheesi.Game.Tests.Features;

public sealed class DisposeHandHandlerTests
{
    private static DisposeHandHandler CreateHandler(IGameRepository repo, ISseConnectionManager? sseManager = null)
    {
        var manager = sseManager ?? new Mock<ISseConnectionManager>().Object;
        return new DisposeHandHandler(repo, manager);
    }

    [Fact]
    public async Task Handle_GameNotFound_ThrowsNotFoundException()
    {
        var mockRepo = new Mock<IGameRepository>();
        mockRepo.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<IGameState?>(null));
        var handler = CreateHandler(mockRepo.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new DisposeHandCommand("MISSING", Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_GameNotInProgress_ThrowsDomainException()
    {
        var game = GameStateFactory.Create(status: GameStatus.Waiting);
        var mockRepo = new Mock<IGameRepository>();
        mockRepo.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);
        var handler = CreateHandler(mockRepo.Object);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new DisposeHandCommand(game.GameCode, Guid.NewGuid()), CancellationToken.None));

        Assert.Contains("not in progress", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_NotPlayersTurn_ThrowsDomainException()
    {
        var (game, _, _) = GameStateFactory.CreateInProgress();
        var otherPlayer = Guid.NewGuid();
        var mockRepo = new Mock<IGameRepository>();
        mockRepo.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);
        var handler = CreateHandler(mockRepo.Object);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new DisposeHandCommand(game.GameCode, otherPlayer), CancellationToken.None));

        Assert.Contains("not your turn", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_PlayerHasPlayableCards_ThrowsDomainException()
    {
        // Player has a pawn on the board and a card with a valid move → has playable cards
        var (game, player, _) = GameStateFactory.CreateInProgress(boardPosition: 5);
        var mockRepo = new Mock<IGameRepository>();
        mockRepo.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);
        var handler = CreateHandler(mockRepo.Object);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new DisposeHandCommand(game.GameCode, player.Id), CancellationToken.None));

        Assert.Contains("playable cards", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_ValidDispose_SavesGameAndBroadcastsTwoSseEvents()
    {
        // Player has 4 finished pawns (no reserve, no in-play) and a card with no valid moves
        var playerId = Guid.NewGuid();
        var pawns = Enumerable.Range(0, 4)
            .Select(i => new Pawn(Guid.NewGuid(), playerId, PawnStatus.Finished, new FinishLocation(i + 1)))
            .ToList();
        var player = new Player(playerId, "Alice", pawns.AsReadOnly());
        var hand = new PlayerHand(playerId, [new Card(CardSuit.Clubs, CardRank.Two)]);
        var turn = new TurnState(playerId, playerId, 1);
        var game = GameStateFactory.Create(
            status: GameStatus.InProgress,
            players: [player],
            turn: turn,
            hands: [hand]);

        var mockRepo = new Mock<IGameRepository>();
        mockRepo.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);
        var mockSse = new Mock<ISseConnectionManager>();
        var handler = CreateHandler(mockRepo.Object, mockSse.Object);

        await handler.Handle(new DisposeHandCommand(game.GameCode, playerId), CancellationToken.None);

        mockRepo.Verify(r => r.SaveAsync(It.IsAny<IGameState>(), It.IsAny<CancellationToken>()), Times.Once);
        mockSse.Verify(
            s => s.BroadcastAsync(game.GameCode, It.Is<SseEvent>(e => e.EventType == "game-updated"), It.IsAny<CancellationToken>()),
            Times.Once);
        mockSse.Verify(
            s => s.BroadcastAsync(game.GameCode, It.Is<SseEvent>(e => e.EventType == "your-turn"), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
