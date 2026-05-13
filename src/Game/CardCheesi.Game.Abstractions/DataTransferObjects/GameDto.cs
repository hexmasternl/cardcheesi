using CardCheesi.Game.Abstractions.DomainModels;

namespace CardCheesi.Game.Abstractions.DataTransferObjects;

public sealed record GameDto(
    Guid Id,
    string GameCode,
    GameStatus Status,
    IReadOnlyList<PlayerInGameDto> Players,
    IReadOnlyList<TeamInGameDto> Teams,
    TurnStateDto? Turn,
    DeckDto? Deck,
    IReadOnlyList<PlayerHandDto>? Hands);

public sealed record PlayerInGameDto(Guid Id, string Name, IReadOnlyList<PawnDto> Pawns);

public sealed record TeamInGameDto(Guid Id, IReadOnlyList<PlayerInGameDto> Players);
