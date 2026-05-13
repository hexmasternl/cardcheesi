using CardCheesi.Game.Abstractions.DomainModels;

namespace CardCheesi.Game.Abstractions.DataTransferObjects;

public sealed record PawnDto(Guid Id, Guid OwnerId, PawnStatus Status, PawnLocation Location);
