using CardCheesi.Game.Abstractions.DomainModels;

namespace CardCheesi.Game.Abstractions.DataTransferObjects;

public sealed record GameDto(
    Guid Id,
    string GameCode,
    GameStatus Status,
    IReadOnlyList<PlayerInGameDto> Players,
    IReadOnlyList<TeamInGameDto> Teams,
    object? Turn,
    object? Deck,
    IReadOnlyList<object>? Hands);

public sealed record PlayerInGameDto(Guid Id, string Name, IReadOnlyList<object> Pawns);

public sealed record TeamInGameDto(Guid Id, IReadOnlyList<PlayerInGameDto> Players);
