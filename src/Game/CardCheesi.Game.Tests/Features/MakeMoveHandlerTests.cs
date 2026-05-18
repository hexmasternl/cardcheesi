using CardCheesi.Core;
using CardCheesi.Game.Abstractions;
using CardCheesi.Game.Abstractions.DataTransferObjects;
using CardCheesi.Game.Abstractions.DomainModels;
using CardCheesi.Game.Features.MakeMove;
using CardCheesi.Game.Tests.Factories;
using Moq;

namespace CardCheesi.Game.Tests.Features;

public sealed class MakeMoveHandlerTests
{
    private static MakeMoveHandler CreateHandler(IGameRepository repo, ISseConnectionManager? sseManager = null)
    {
        var manager = sseManager ?? new Mock<ISseConnectionManager>().Object;
        return new MakeMoveHandler(repo, manager);
    }

    [Fact]
    public async Task Handle_GameNotFound_ThrowsNotFoundException()
    {
        var mockRepo = new Mock<IGameRepository>();
        mockRepo.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<IGameState?>(null));
        var handler = CreateHandler(mockRepo.Object);

        var request = new MakeMoveRequest(0, 2, Guid.NewGuid(), null, 2);
        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new MakeMoveCommand("MISSING", Guid.NewGuid(), request), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_GameNotInProgress_ThrowsDomainException()
    {
        var game = GameStateFactory.Create(status: GameStatus.Waiting);
        var mockRepo = new Mock<IGameRepository>();
        mockRepo.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);
        var handler = CreateHandler(mockRepo.Object);

        var request = new MakeMoveRequest(0, 2, Guid.NewGuid(), null, 2);
        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new MakeMoveCommand(game.GameCode, Guid.NewGuid(), request), CancellationToken.None));

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

        var request = new MakeMoveRequest(0, 2, Guid.NewGuid(), null, 2);
        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new MakeMoveCommand(game.GameCode, otherPlayer, request), CancellationToken.None));

        Assert.Contains("not your turn", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_JackMissingSecondPawn_ThrowsDomainException()
    {
        var (game, player, pawn) = GameStateFactory.CreateInProgress(
            cards: [new Card(CardSuit.Clubs, CardRank.Jack), new Card(CardSuit.Hearts, CardRank.Two)]);
        var mockRepo = new Mock<IGameRepository>();
        mockRepo.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);
        var handler = CreateHandler(mockRepo.Object);

        // Jack without PawnId2 should throw
        var request = new MakeMoveRequest(
            (int)CardSuit.Clubs, (int)CardRank.Jack, pawn.Id, PawnId2: null, Steps: null);

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new MakeMoveCommand(game.GameCode, player.Id, request), CancellationToken.None));

        Assert.Contains("Jack requires two pawn IDs", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_ValidMove_SavesGameAndBroadcastsTwoSseEvents()
    {
        // 2 cards in hand so after playing one, hand is not empty → AdvanceTurn just moves turn
        var (game, player, pawn) = GameStateFactory.CreateInProgress(boardPosition: 5);
        var mockRepo = new Mock<IGameRepository>();
        mockRepo.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);
        var mockSse = new Mock<ISseConnectionManager>();
        var handler = CreateHandler(mockRepo.Object, mockSse.Object);

        // Move pawn 2 steps forward from position 5 (→ 7)
        var request = new MakeMoveRequest(
            (int)CardSuit.Clubs, (int)CardRank.Two, pawn.Id, PawnId2: null, Steps: 2);

        await handler.Handle(new MakeMoveCommand(game.GameCode, player.Id, request), CancellationToken.None);

        mockRepo.Verify(r => r.SaveAsync(It.IsAny<IGameState>(), It.IsAny<CancellationToken>()), Times.Once);
        mockSse.Verify(
            s => s.BroadcastAsync(game.GameCode, It.Is<SseEvent>(e => e.EventType == "game-updated"), It.IsAny<CancellationToken>()),
            Times.Once);
        mockSse.Verify(
            s => s.BroadcastAsync(game.GameCode, It.Is<SseEvent>(e => e.EventType == "your-turn"), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
