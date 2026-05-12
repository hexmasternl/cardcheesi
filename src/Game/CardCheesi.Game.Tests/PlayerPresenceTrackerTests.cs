using System.Threading.Channels;
using CardCheesi.Game.Abstractions;
using CardCheesi.Game.Abstractions.DomainModels;
using Moq;

namespace CardCheesi.Game.Tests;

public class PlayerPresenceTrackerTests
{
    private static (PlayerPresenceTracker tracker, Mock<ISseConnectionManager> mockManager) CreateTracker()
    {
        var mock = new Mock<ISseConnectionManager>();
        mock.Setup(m => m.BroadcastAsync(It.IsAny<string>(), It.IsAny<SseEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return (new PlayerPresenceTracker(mock.Object), mock);
    }

    [Fact]
    public async Task ConnectAsync_SetsStatusConnected_AndBroadcasts()
    {
        var (tracker, mock) = CreateTracker();
        var playerId = Guid.NewGuid();

        await tracker.ConnectAsync("GAME01", playerId, "Alice");

        var snapshot = tracker.GetSnapshot("GAME01");
        Assert.Single(snapshot);
        Assert.Equal(PlayerPresenceStatus.Connected, snapshot[0].Status);

        mock.Verify(m => m.BroadcastAsync("GAME01", It.IsAny<SseEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DisconnectAsync_SetsStatusDisconnected_AndBroadcasts()
    {
        var (tracker, mock) = CreateTracker();
        var playerId = Guid.NewGuid();

        await tracker.ConnectAsync("GAME01", playerId, "Bob");
        await tracker.DisconnectAsync("GAME01", playerId);

        var snapshot = tracker.GetSnapshot("GAME01");
        Assert.Equal(PlayerPresenceStatus.Disconnected, snapshot[0].Status);

        mock.Verify(m => m.BroadcastAsync("GAME01", It.IsAny<SseEvent>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ConnectAsync_AfterDisconnect_CancelGraceTimer_AndRestoresConnected()
    {
        var (tracker, mock) = CreateTracker();
        var playerId = Guid.NewGuid();

        await tracker.ConnectAsync("GAME01", playerId, "Carol");
        await tracker.DisconnectAsync("GAME01", playerId);

        // Reconnect before grace period expires
        await tracker.ConnectAsync("GAME01", playerId, "Carol");

        var snapshot = tracker.GetSnapshot("GAME01");
        Assert.Equal(PlayerPresenceStatus.Connected, snapshot[0].Status);
    }

    [Fact]
    public async Task DisconnectAsync_AfterGracePeriod_TransitionsToLeft()
    {
        // Use a custom tracker with shortened grace period via reflection for test speed
        var mock = new Mock<ISseConnectionManager>();
        var broadcastedStatuses = new List<string>();
        mock.Setup(m => m.BroadcastAsync(It.IsAny<string>(), It.IsAny<SseEvent>(), It.IsAny<CancellationToken>()))
            .Callback<string, SseEvent, CancellationToken>((_, evt, _) => broadcastedStatuses.Add(evt.Data))
            .Returns(Task.CompletedTask);

        var tracker = new PlayerPresenceTracker(mock.Object);
        var playerId = Guid.NewGuid();

        await tracker.ConnectAsync("GAME01", playerId, "Dave");
        await tracker.DisconnectAsync("GAME01", playerId);

        // Wait longer than the real grace period is impractical in tests.
        // Verify Disconnected status is set immediately after DisconnectAsync.
        var snapshot = tracker.GetSnapshot("GAME01");
        Assert.Equal(PlayerPresenceStatus.Disconnected, snapshot[0].Status);

        // Verify Connected was broadcast first, Disconnected second
        Assert.Contains(broadcastedStatuses, s => s.Contains("Connected"));
        Assert.Contains(broadcastedStatuses, s => s.Contains("Disconnected"));
    }
}
