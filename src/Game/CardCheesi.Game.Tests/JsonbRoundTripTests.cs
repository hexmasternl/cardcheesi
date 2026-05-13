using System.Text.Json;
using CardCheesi.Game.Abstractions.DomainModels;
using CardCheesi.Game.DomainModels;

namespace CardCheesi.Game.Tests;

public sealed class JsonbRoundTripTests
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    [Fact]
    public void PawnLocation_AllTypes_RoundTrip()
    {
        PawnLocation[] locations =
        [
            new ReserveLocation(),
            new BoardLocation(17),
            new FinishLocation(3)
        ];

        foreach (var location in locations)
        {
            var json = JsonSerializer.Serialize(location, Options);
            var restored = JsonSerializer.Deserialize<PawnLocation>(json, Options);

            Assert.NotNull(restored);
            Assert.Equal(location, restored);
        }
    }

    [Fact]
    public void GameState_WaitingWithSinglePlayer_RoundTrip()
    {
        var original = GameFactory.CreateWaiting("Alice", "ABC123");

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<GameState>(json, Options);

        Assert.NotNull(restored);
        Assert.Equal(original.Id, restored.Id);
        Assert.Equal(original.GameCode, restored.GameCode);
        Assert.Equal(original.Status, restored.Status);
        Assert.Equal(original.Players[0].Name, restored.Players[0].Name);
        Assert.Null(restored.Turn);
        Assert.Null(restored.Deck);
        Assert.Null(restored.Hands);
    }

    [Fact]
    public void GameState_FullGame_RoundTrip()
    {
        var original = GameFactory.Create(["Alice", "Bob", "Carol", "Dave"], "XYZABC");

        var json = JsonSerializer.Serialize(original, Options);
        var restored = JsonSerializer.Deserialize<GameState>(json, Options);

        Assert.NotNull(restored);
        Assert.Equal(original.Id, restored.Id);
        Assert.Equal(4, restored.Players.Count);
        Assert.NotNull(restored.Turn);
        Assert.NotNull(restored.Deck);
        Assert.Equal(52, restored.Deck.Cards.Count);
        Assert.NotNull(restored.Hands);
    }

    [Fact]
    public void Pawn_WithBoardLocation_RoundTrip()
    {
        var pawn = new Pawn(Guid.NewGuid(), Guid.NewGuid(), PawnStatus.InPlay, new BoardLocation(42));

        var json = JsonSerializer.Serialize(pawn, Options);
        var restored = JsonSerializer.Deserialize<Pawn>(json, Options);

        Assert.NotNull(restored);
        Assert.IsType<BoardLocation>(restored.Location);
        Assert.Equal(42, ((BoardLocation)restored.Location).Position);
    }

    [Fact]
    public void Pawn_WithFinishLocation_RoundTrip()
    {
        var pawn = new Pawn(Guid.NewGuid(), Guid.NewGuid(), PawnStatus.Finished, new FinishLocation(2));

        var json = JsonSerializer.Serialize(pawn, Options);
        var restored = JsonSerializer.Deserialize<Pawn>(json, Options);

        Assert.NotNull(restored);
        Assert.IsType<FinishLocation>(restored.Location);
        Assert.Equal(2, ((FinishLocation)restored.Location).Slot);
    }
}
