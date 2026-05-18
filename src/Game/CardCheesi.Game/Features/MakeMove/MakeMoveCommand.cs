using CardCheesi.Game.Abstractions.DataTransferObjects;

namespace CardCheesi.Game.Features.MakeMove;

public sealed record MakeMoveCommand(string GameCode, Guid PlayerId, MakeMoveRequest Request);
public sealed record MakeMoveResult;
