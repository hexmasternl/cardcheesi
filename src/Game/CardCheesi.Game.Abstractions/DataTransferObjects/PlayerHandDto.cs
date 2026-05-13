using CardCheesi.Game.Abstractions.DomainModels;

namespace CardCheesi.Game.Abstractions.DataTransferObjects;

public sealed record PlayerHandDto(Guid PlayerId, IReadOnlyList<Card> Cards);
