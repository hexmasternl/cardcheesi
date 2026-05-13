using CardCheesi.Core;
using CardCheesi.Game.Abstractions;
using CardCheesi.Game.Abstractions.DomainModels;
using CardCheesi.Game.Features.JoinGame;
using CardCheesi.Game.Tests.Factories;
using Moq;

namespace CardCheesi.Game.Tests.Features;

public sealed class JoinGameHandlerTests
{
    private static JoinGameHandler CreateHandler(IGameRepository repo, ISseConnectionManager? sseManager = null)
    {
        var manager = sseManager ?? new Mock<ISseConnectionManager>().Object;
        return new JoinGameHandler(repo, manager);
    }

    [Fact]
    public async Task Handle_GameNotFound_ThrowsNotFoundException()
    {
        var mockRepo = new Mock<IGameRepository>();
        mockRepo.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<IGameState?>(null));
        var handler = CreateHandler(mockRepo.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new JoinGameCommand("ABC123", Guid.NewGuid(), "Alice"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_PlayerAlreadyInGame_ThrowsDomainException()
    {
        var playerId = Guid.NewGuid();
        var player = PlayerFactory.Create(id: playerId, name: "Alice");
        var game = GameStateFactory.Create(players: [player]);
        var mockRepo = new Mock<IGameRepository>();
        mockRepo.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);
        var handler = CreateHandler(mockRepo.Object);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new JoinGameCommand("ABC123", playerId, "Alice"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_SuccessfulJoin_BroadcastsPlayerJoinedEvent()
    {
        var game = GameStateFactory.Create();
        var mockRepo = new Mock<IGameRepository>();
        mockRepo.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);
        mockRepo.Setup(r => r.SaveAsync(It.IsAny<IGameState>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mockSse = new Mock<ISseConnectionManager>();
        var handler = CreateHandler(mockRepo.Object, mockSse.Object);

        var newPlayerId = Guid.NewGuid();
        await handler.Handle(new JoinGameCommand(game.GameCode, newPlayerId, "Bob"), CancellationToken.None);

        mockSse.Verify(
            s => s.BroadcastAsync(
                game.GameCode,
                It.Is<SseEvent>(e => e.EventType == "player-joined" && e.Data.Contains(newPlayerId.ToString())),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_GameFull_DoesNotBroadcast()
    {
        var players = Enumerable.Range(0, 4).Select(_ => PlayerFactory.Create()).ToList();
        var game = GameStateFactory.Create(players: players);
        var mockRepo = new Mock<IGameRepository>();
        mockRepo.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);

        var mockSse = new Mock<ISseConnectionManager>();
        var handler = CreateHandler(mockRepo.Object, mockSse.Object);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new JoinGameCommand(game.GameCode, Guid.NewGuid(), "Carol"), CancellationToken.None));

        mockSse.Verify(
            s => s.BroadcastAsync(It.IsAny<string>(), It.IsAny<SseEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_GameNotWaiting_DoesNotBroadcast()
    {
        var game = GameStateFactory.Create(status: Abstractions.DomainModels.GameStatus.InProgress);
        var mockRepo = new Mock<IGameRepository>();
        mockRepo.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);

        var mockSse = new Mock<ISseConnectionManager>();
        var handler = CreateHandler(mockRepo.Object, mockSse.Object);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new JoinGameCommand(game.GameCode, Guid.NewGuid(), "Dave"), CancellationToken.None));

        mockSse.Verify(
            s => s.BroadcastAsync(It.IsAny<string>(), It.IsAny<SseEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
