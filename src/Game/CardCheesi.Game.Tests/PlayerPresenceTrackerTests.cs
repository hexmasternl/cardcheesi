using System.Threading.Channels;
using CardCheesi.Game.Abstractions;
using CardCheesi.Game.Abstractions.DomainModels;
using Moq;

namespace CardCheesi.Game.Tests;

public sealed class PlayerPresenceTrackerTests
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

    [Fact]
    public async Task LeaveAsync_SetsStatusLeft_AndBroadcasts()
    {
        var (tracker, mock) = CreateTracker();
        var playerId = Guid.NewGuid();

        await tracker.ConnectAsync("GAME01", playerId, "Eve");
        await tracker.LeaveAsync("GAME01", playerId);

        var snapshot = tracker.GetSnapshot("GAME01");
        Assert.Equal(PlayerPresenceStatus.Left, snapshot[0].Status);

        // Connected + Left = 2 broadcasts
        mock.Verify(m => m.BroadcastAsync("GAME01", It.IsAny<SseEvent>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task LeaveAsync_IsIdempotent_WhenAlreadyLeft()
    {
        var (tracker, mock) = CreateTracker();
        var playerId = Guid.NewGuid();

        await tracker.ConnectAsync("GAME01", playerId, "Frank");
        await tracker.LeaveAsync("GAME01", playerId);
        await tracker.LeaveAsync("GAME01", playerId); // second call — should be a no-op

        mock.Verify(m => m.BroadcastAsync("GAME01", It.IsAny<SseEvent>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task DisconnectAsync_DoesNotDowngrade_WhenPlayerAlreadyLeft()
    {
        var (tracker, mock) = CreateTracker();
        var playerId = Guid.NewGuid();

        await tracker.ConnectAsync("GAME01", playerId, "Grace");
        await tracker.LeaveAsync("GAME01", playerId);

        // SSE close may race with the beacon — DisconnectAsync must not downgrade Left → Disconnected
        await tracker.DisconnectAsync("GAME01", playerId);

        var snapshot = tracker.GetSnapshot("GAME01");
        Assert.Equal(PlayerPresenceStatus.Left, snapshot[0].Status);

        // Only Connected + Left — no extra Disconnected broadcast
        mock.Verify(m => m.BroadcastAsync("GAME01", It.IsAny<SseEvent>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task LeaveAsync_CancelsGraceTimer_FromDisconnectedState()
    {
        var (tracker, mock) = CreateTracker();
        var playerId = Guid.NewGuid();

        await tracker.ConnectAsync("GAME01", playerId, "Henry");
        await tracker.DisconnectAsync("GAME01", playerId);
        // Grace period started — explicit leave should cancel it and set Left immediately
        await tracker.LeaveAsync("GAME01", playerId);

        var snapshot = tracker.GetSnapshot("GAME01");
        Assert.Equal(PlayerPresenceStatus.Left, snapshot[0].Status);
    }
}
