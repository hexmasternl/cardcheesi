namespace CardCheesi.Game.Abstractions.DomainModels;

public abstract record MoveOption;

/// <summary>Move a single pawn forward (positive steps) or backward (negative steps), or enter home (steps == 0).</summary>
public record SingleMove(Guid PawnId, int Steps) : MoveOption;

/// <summary>Split a Seven card across two pawns: Steps1 + Steps2 == 7.</summary>
public record SplitMove(Guid PawnId1, int Steps1, Guid? PawnId2, int Steps2) : MoveOption;

/// <summary>Jack card: swap positions of two pawns belonging to different owners.</summary>
public record SwapMove(Guid PawnId1, Guid PawnId2) : MoveOption;
