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
        return game is null ? null : MapToDto(game);
    }

    private static GameDto MapToDto(IGameState game)
    {
        var playerDtos = game.Players
            .Select(p => new PlayerInGameDto(p.Id, p.Name, p.Pawns.Cast<object>().ToList()))
            .ToList();

        var playerDtoMap = playerDtos.ToDictionary(p => p.Id);

        var teamDtos = game.Teams
            .Select(t => new TeamInGameDto(
                t.Id,
                t.Players.Select(p => playerDtoMap.TryGetValue(p.Id, out var dto)
                    ? dto
                    : new PlayerInGameDto(p.Id, p.Name, p.Pawns.Cast<object>().ToList())).ToList()))
            .ToList();

        return new GameDto(
            game.Id,
            game.GameCode,
            game.Status,
            playerDtos,
            teamDtos,
            game.Turn,
            game.Deck,
            game.Hands?.Cast<object>().ToList());
    }
}
