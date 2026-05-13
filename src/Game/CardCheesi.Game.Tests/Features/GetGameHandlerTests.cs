using CardCheesi.Core;
using CardCheesi.Game.Abstractions;
using CardCheesi.Game.Abstractions.DomainModels;
using CardCheesi.Game.Features.GetGame;
using CardCheesi.Game.Tests.Factories;
using Moq;

namespace CardCheesi.Game.Tests.Features;

public sealed class GetGameHandlerTests
{
    [Fact]
    public async Task Handle_GameNotFound_ReturnsNull()
    {
        var mockRepo = new Mock<IGameRepository>();
        mockRepo.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<IGameState?>(null));
        var handler = new GetGameHandler(mockRepo.Object);

        var result = await handler.Handle(new GetGameQuery("ABC123", Guid.NewGuid()), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_PlayerNotInGame_ThrowsForbiddenException()
    {
        var game = GameStateFactory.Create();
        var mockRepo = new Mock<IGameRepository>();
        mockRepo.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);
        var handler = new GetGameHandler(mockRepo.Object);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new GetGameQuery("ABC123", Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_PlayerInGame_ReturnsGameDto()
    {
        var playerId = Guid.NewGuid();
        var player = PlayerFactory.Create(id: playerId);
        var game = GameStateFactory.Create(players: [player]);
        var mockRepo = new Mock<IGameRepository>();
        mockRepo.Setup(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);
        var handler = new GetGameHandler(mockRepo.Object);

        var result = await handler.Handle(new GetGameQuery("ABC123", playerId), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(game.Id, result.Id);
    }
}
