using CardCheesi.Core;
using CardCheesi.Game.Abstractions;
using CardCheesi.Game.Abstractions.DataTransferObjects;
using CardCheesi.Game.Abstractions.DomainModels;

namespace CardCheesi.Game.Features.GetGame;

public sealed class GetGameHandler : IQueryHandler<GetGameQuery, GameDto?>
{
    private readonly IGameRepository _repo;

    public GetGameHandler(IGameRepository repo) => _repo = repo;

    public async Task<GameDto?> Handle(GetGameQuery query, CancellationToken ct)
    {
        var game = await _repo.GetByCodeAsync(query.GameCode, ct);
        if (game is null) return null;

        if (game.Players.All(p => p.Id != query.RequestingPlayerId))
            throw new ForbiddenException("You are not a player in this game.");

        return MapToDto(game);
    }

    private static GameDto MapToDto(IGameState game)
    {
        var playerDtos = game.Players
            .Select(p => new PlayerInGameDto(
                p.Id,
                p.Name,
                p.Pawns.Select(pawn => new PawnDto(pawn.Id, pawn.OwnerId, pawn.Status, pawn.Location)).ToList()))
            .ToList();

        var playerDtoMap = playerDtos.ToDictionary(p => p.Id);

        var teamDtos = game.Teams
            .Select(t => new TeamInGameDto(
                t.Id,
                t.Players.Select(p => playerDtoMap.TryGetValue(p.Id, out var dto)
                    ? dto
                    : new PlayerInGameDto(
                        p.Id,
                        p.Name,
                        p.Pawns.Select(pawn => new PawnDto(pawn.Id, pawn.OwnerId, pawn.Status, pawn.Location)).ToList()))
                    .ToList()))
            .ToList();

        var turnDto = game.Turn is null ? null
            : new TurnStateDto(game.Turn.ActivePlayerId, game.Turn.DealerId, game.Turn.RoundNumber, game.Turn.CardsThisRound);

        var deckDto = game.Deck is null ? null
            : new DeckDto(game.Deck.Cards);

        var handDtos = game.Hands?
            .Select(h => new PlayerHandDto(h.PlayerId, h.Cards))
            .ToList();

        return new GameDto(
            game.Id,
            game.GameCode,
            game.Status,
            playerDtos,
            teamDtos,
            turnDto,
            deckDto,
            handDtos);
    }
}

