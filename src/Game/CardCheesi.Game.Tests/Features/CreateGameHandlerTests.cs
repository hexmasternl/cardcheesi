using CardCheesi.Game.Abstractions;
using CardCheesi.Game.Abstractions.DomainModels;
using CardCheesi.Game.Features.CreateGame;
using Moq;

namespace CardCheesi.Game.Tests.Features;

public sealed class CreateGameHandlerTests
{
    [Fact]
    public async Task Handle_ValidCommand_ReturnsGameIdAndCode()
    {
        var mockRepo = new Mock<IGameRepository>();
        var handler = new CreateGameHandler(mockRepo.Object);
        var command = new CreateGameCommand("Alice", Guid.NewGuid());

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.GameId);
        Assert.NotEmpty(result.GameCode);
        Assert.Equal(6, result.GameCode.Length);
    }

    [Fact]
    public async Task Handle_ValidCommand_SavesGameToRepository()
    {
        var mockRepo = new Mock<IGameRepository>();
        var handler = new CreateGameHandler(mockRepo.Object);
        var command = new CreateGameCommand("Alice", Guid.NewGuid());

        await handler.Handle(command, CancellationToken.None);

        mockRepo.Verify(
            r => r.SaveAsync(It.IsAny<IGameState>(), It.IsAny<CancellationToken>()),
            Times.Once);    }
}
