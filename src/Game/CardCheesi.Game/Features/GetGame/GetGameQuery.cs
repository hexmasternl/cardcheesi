using CardCheesi.Game.Abstractions.DataTransferObjects;

namespace CardCheesi.Game.Features.GetGame;

public sealed record GetGameQuery(string GameCode, Guid RequestingPlayerId);
