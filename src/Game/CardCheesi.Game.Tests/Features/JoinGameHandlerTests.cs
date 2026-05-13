using CardCheesi.Core;
using CardCheesi.Game.Abstractions;
using CardCheesi.Game.Abstractions.DomainModels;
using CardCheesi.Game.Features.JoinGame;
using CardCheesi.Game.Tests.Factories;
using Moq;

namespace CardCheesi.Game.Tests.Features;

public sealed class JoinGameHandlerTests
{
    [Fact]
    public async Task Handle_GameNotFound_ThrowsNotFoundException()
    {
        var mockRepo = new Mock<IGameRepository>();
        mockRepo.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<IGameState?>(null));
        var handler = new JoinGameHandler(mockRepo.Object);

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
        var handler = new JoinGameHandler(mockRepo.Object);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new JoinGameCommand("ABC123", playerId, "Alice"), CancellationToken.None));
    }
}
