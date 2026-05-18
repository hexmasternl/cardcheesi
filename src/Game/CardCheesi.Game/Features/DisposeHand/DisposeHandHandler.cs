using System.Text.Json;
using CardCheesi.Core;
using CardCheesi.Game.Abstractions;
using CardCheesi.Game.Abstractions.DomainModels;
using CardCheesi.Game.DomainModels;

namespace CardCheesi.Game.Features.DisposeHand;

public sealed class DisposeHandHandler : ICommandHandler<DisposeHandCommand, DisposeHandResult>
{
    private readonly IGameRepository _repo;
    private readonly ISseConnectionManager _sseConnectionManager;

    public DisposeHandHandler(IGameRepository repo, ISseConnectionManager sseConnectionManager)
    {
        _repo = repo;
        _sseConnectionManager = sseConnectionManager;
    }

    public async Task<DisposeHandResult> Handle(DisposeHandCommand command, CancellationToken ct)
    {
        var game = await _repo.GetByCodeAsync(command.GameCode, ct)
            ?? throw new NotFoundException($"Game with code '{command.GameCode}' not found.");

        if (game.Status != GameStatus.InProgress)
            throw new DomainException("Game is not in progress.");

        if (game.Turn?.ActivePlayerId != command.PlayerId)
            throw new DomainException("It is not your turn.");

        if (game.HasPlayableCards(command.PlayerId))
            throw new DomainException("You have playable cards and must take a turn instead of disposing.");

        var gameState = (GameState)game;
        var afterDispose = gameState.DisposeHand(command.PlayerId);
        var finalState = afterDispose.AdvanceTurn();

        await _repo.SaveAsync(finalState, ct);

        await BroadcastTurnAsync(command.GameCode, finalState, ct);

        return new DisposeHandResult();
    }

    private async Task BroadcastTurnAsync(string gameCode, GameState state, CancellationToken ct)
    {
        await _sseConnectionManager.BroadcastAsync(gameCode, new SseEvent("game-updated", "{}"), ct);

        if (state.Turn is null) return;

        var nextPlayerId = state.Turn.ActivePlayerId;
        bool canDispose = !state.HasPlayableCards(nextPlayerId);
        var yourTurnPayload = JsonSerializer.Serialize(new
        {
            activePlayerId = nextPlayerId.ToString(),
            canDispose,
        });
        await _sseConnectionManager.BroadcastAsync(gameCode, new SseEvent("your-turn", yourTurnPayload), ct);
    }
}
