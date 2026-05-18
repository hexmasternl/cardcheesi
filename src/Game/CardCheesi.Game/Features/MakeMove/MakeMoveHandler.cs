using System.Text.Json;
using CardCheesi.Core;
using CardCheesi.Game.Abstractions;
using CardCheesi.Game.Abstractions.DataTransferObjects;
using CardCheesi.Game.Abstractions.DomainModels;
using CardCheesi.Game.DomainModels;

namespace CardCheesi.Game.Features.MakeMove;

public sealed class MakeMoveHandler : ICommandHandler<MakeMoveCommand, MakeMoveResult>
{
    private readonly IGameRepository _repo;
    private readonly ISseConnectionManager _sseConnectionManager;

    public MakeMoveHandler(IGameRepository repo, ISseConnectionManager sseConnectionManager)
    {
        _repo = repo;
        _sseConnectionManager = sseConnectionManager;
    }

    public async Task<MakeMoveResult> Handle(MakeMoveCommand command, CancellationToken ct)
    {
        var game = await _repo.GetByCodeAsync(command.GameCode, ct)
            ?? throw new NotFoundException($"Game with code '{command.GameCode}' not found.");

        if (game.Status != GameStatus.InProgress)
            throw new DomainException("Game is not in progress.");

        if (game.Turn?.ActivePlayerId != command.PlayerId)
            throw new DomainException("It is not your turn.");

        var request = command.Request;
        var card = new Card((CardSuit)request.CardSuit, (CardRank)request.CardRank);
        var rank = (CardRank)request.CardRank;

        var gameState = (GameState)game;
        GameState afterCard = gameState.PlayCard(command.PlayerId, card);

        GameState afterMove = rank switch
        {
            CardRank.Jack when request.PawnId2.HasValue
                => afterCard.SwapPawns(request.PawnId, request.PawnId2.Value),

            CardRank.Jack
                => throw new DomainException("Jack requires two pawn IDs."),

            CardRank.Seven when request.PawnId2.HasValue
                => afterCard.MakeSplitMove(
                    request.PawnId,
                    request.Steps ?? throw new DomainException("Seven split requires steps."),
                    request.PawnId2,
                    7 - (request.Steps ?? 0)),

            CardRank.Seven
                => afterCard.MakeMove(request.PawnId, 7),

            _ => afterCard.MakeMove(
                request.PawnId,
                request.Steps ?? throw new DomainException("Steps required for this card.")),
        };

        var finalState = afterMove.AdvanceTurn();
        await _repo.SaveAsync(finalState, ct);

        await BroadcastTurnAsync(command.GameCode, finalState, ct);

        return new MakeMoveResult();
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
