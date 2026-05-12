using System.Threading.Channels;
using CardCheesi.Game.Abstractions;
using CardCheesi.Game.Abstractions.DomainModels;

namespace CardCheesi.Game.Tests;

public class SseConnectionManagerTests
{
    [Fact]
    public async Task BroadcastAsync_SendsEventToAllChannelsForGame()
    {
        var manager = new SseConnectionManager();
        var ch1 = Channel.CreateUnbounded<SseEvent>();
        var ch2 = Channel.CreateUnbounded<SseEvent>();
        manager.AddConnection("GAME01", ch1);
        manager.AddConnection("GAME01", ch2);

        var evt = new SseEvent("player-status", "{\"status\":\"Connected\"}");
        await manager.BroadcastAsync("GAME01", evt);

        Assert.True(ch1.Reader.TryRead(out var r1));
        Assert.Equal("player-status", r1.EventType);

        Assert.True(ch2.Reader.TryRead(out var r2));
        Assert.Equal("player-status", r2.EventType);
    }

    [Fact]
    public async Task BroadcastAsync_DoesNotSendToChannelsForOtherGames()
    {
        var manager = new SseConnectionManager();
        var ch1 = Channel.CreateUnbounded<SseEvent>();
        var ch2 = Channel.CreateUnbounded<SseEvent>();
        manager.AddConnection("GAME01", ch1);
        manager.AddConnection("GAME02", ch2);

        var evt = new SseEvent("player-status", "{}");
        await manager.BroadcastAsync("GAME01", evt);

        Assert.True(ch1.Reader.TryRead(out _));
        Assert.False(ch2.Reader.TryRead(out _));
    }

    [Fact]
    public async Task RemoveConnection_StopsDelivery()
    {
        var manager = new SseConnectionManager();
        var ch = Channel.CreateUnbounded<SseEvent>();
        manager.AddConnection("GAME01", ch);
        manager.RemoveConnection("GAME01", ch);

        await manager.BroadcastAsync("GAME01", new SseEvent("player-status", "{}"));

        Assert.False(ch.Reader.TryRead(out _));
    }
}
