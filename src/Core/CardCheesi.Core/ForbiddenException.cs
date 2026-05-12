namespace CardCheesi.Core;

public sealed class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
}
