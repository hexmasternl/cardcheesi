namespace CardCheesi.Game.Abstractions;

public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
