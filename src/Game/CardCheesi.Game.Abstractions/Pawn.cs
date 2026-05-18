namespace CardCheesi.Game.Abstractions.DomainModels;

public interface IPawn
{
    Guid Id { get; }
    Guid OwnerId { get; }
    PawnStatus Status { get; }
    PawnLocation Location { get; }
    bool IsProtected { get; }
}
