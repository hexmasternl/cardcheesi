using CardCheesi.Game.Abstractions.DomainModels;

namespace CardCheesi.Game.Abstractions.DataTransferObjects;

public sealed record DeckDto(IReadOnlyList<Card> Cards);
