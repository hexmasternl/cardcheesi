using CardCheesi.Core;
using CardCheesi.Game.Abstractions;
using CardCheesi.Game.Abstractions.DomainModels;
using CardCheesi.Game.Features.Chat;
using CardCheesi.Game.Tests.Factories;
using Moq;

namespace CardCheesi.Game.Tests.Features;

public sealed class SendChatMessageHandlerTests
{
    private static SendChatMessageHandler CreateHandler(
        IGameRepository repo,
        ISseConnectionManager? sseManager = null)
    {
        var manager = sseManager ?? new Mock<ISseConnectionManager>().Object;
        return new SendChatMessageHandler(repo, manager);
    }

    [Fact]
    public async Task Handle_ValidMessage_BroadcastsChatMessageEvent()
    {
        var playerId = Guid.NewGuid();
        var player = PlayerFactory.Create(id: playerId, name: "Alice");
        var game = GameStateFactory.Create(players: [player]);

        var mockRepo = new Mock<IGameRepository>();
        mockRepo.Setup(r => r.GetByCodeAsync(game.GameCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);

        var mockSse = new Mock<ISseConnectionManager>();
        var handler = CreateHandler(mockRepo.Object, mockSse.Object);

        await handler.Handle(
            new SendChatMessageCommand(game.GameCode, playerId, "Alice", "Hello!"),
            CancellationToken.None);

        mockSse.Verify(
            s => s.BroadcastAsync(
                game.GameCode,
                It.Is<SseEvent>(e => e.EventType == "chat-message" && e.Data.Contains("Hello!")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_EmptyText_ThrowsDomainException()
    {
        var mockRepo = new Mock<IGameRepository>();
        var handler = CreateHandler(mockRepo.Object);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(
                new SendChatMessageCommand("ABC123", Guid.NewGuid(), "Alice", ""),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhitespaceText_ThrowsDomainException()
    {
        var mockRepo = new Mock<IGameRepository>();
        var handler = CreateHandler(mockRepo.Object);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(
                new SendChatMessageCommand("ABC123", Guid.NewGuid(), "Alice", "   "),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_TextExceedsMaxLength_ThrowsDomainException()
    {
        var mockRepo = new Mock<IGameRepository>();
        var handler = CreateHandler(mockRepo.Object);

        var longText = new string('A', 501);
        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(
                new SendChatMessageCommand("ABC123", Guid.NewGuid(), "Alice", longText),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_TextAtMaxLength_Succeeds()
    {
        var playerId = Guid.NewGuid();
        var player = PlayerFactory.Create(id: playerId, name: "Alice");
        var game = GameStateFactory.Create(players: [player]);

        var mockRepo = new Mock<IGameRepository>();
        mockRepo.Setup(r => r.GetByCodeAsync(game.GameCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);

        var mockSse = new Mock<ISseConnectionManager>();
        var handler = CreateHandler(mockRepo.Object, mockSse.Object);

        var maxText = new string('A', 500);
        await handler.Handle(
            new SendChatMessageCommand(game.GameCode, playerId, "Alice", maxText),
            CancellationToken.None);

        mockSse.Verify(
            s => s.BroadcastAsync(
                game.GameCode,
                It.Is<SseEvent>(e => e.EventType == "chat-message"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_GameNotFound_ThrowsNotFoundException()
    {
        var mockRepo = new Mock<IGameRepository>();
        mockRepo.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<IGameState?>(null));

        var handler = CreateHandler(mockRepo.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new SendChatMessageCommand("NOEXIST", Guid.NewGuid(), "Alice", "Hello"),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_PlayerNotInGame_ThrowsForbiddenException()
    {
        var game = GameStateFactory.Create();

        var mockRepo = new Mock<IGameRepository>();
        mockRepo.Setup(r => r.GetByCodeAsync(game.GameCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);

        var handler = CreateHandler(mockRepo.Object);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(
                new SendChatMessageCommand(game.GameCode, Guid.NewGuid(), "Stranger", "Hello"),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ValidMessage_BroadcastPayloadContainsSenderAndTimestamp()
    {
        var playerId = Guid.NewGuid();
        var player = PlayerFactory.Create(id: playerId, name: "Alice");
        var game = GameStateFactory.Create(players: [player]);

        var mockRepo = new Mock<IGameRepository>();
        mockRepo.Setup(r => r.GetByCodeAsync(game.GameCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);

        var mockSse = new Mock<ISseConnectionManager>();
        var handler = CreateHandler(mockRepo.Object, mockSse.Object);

        await handler.Handle(
            new SendChatMessageCommand(game.GameCode, playerId, "Alice", "Test message"),
            CancellationToken.None);

        mockSse.Verify(
            s => s.BroadcastAsync(
                game.GameCode,
                It.Is<SseEvent>(e =>
                    e.EventType == "chat-message" &&
                    e.Data.Contains(playerId.ToString()) &&
                    e.Data.Contains("Alice") &&
                    e.Data.Contains("Test message")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
