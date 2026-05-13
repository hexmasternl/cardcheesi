namespace CardCheesi.Game.Abstractions.DataTransferObjects;

public sealed record TurnStateDto(Guid ActivePlayerId, Guid DealerId, int RoundNumber, int CardsThisRound);
