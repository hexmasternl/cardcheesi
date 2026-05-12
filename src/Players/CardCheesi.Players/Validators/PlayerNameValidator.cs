namespace CardCheesi.Players.Validators;

public static class PlayerNameValidator
{
    public static Dictionary<string, string[]>? Validate(string? name)
    {
        if (string.IsNullOrEmpty(name))
            return new Dictionary<string, string[]> { ["name"] = ["Name is required."] };

        if (name != name.Trim())
            return new Dictionary<string, string[]> { ["name"] = ["Name must not have leading or trailing whitespace."] };

        if (name.Any(c => c < 0x20))
            return new Dictionary<string, string[]> { ["name"] = ["Name must not contain control characters."] };

        if (name.Length > 50)
            return new Dictionary<string, string[]> { ["name"] = [$"Name must not exceed 50 characters (was {name.Length})."] };

        return null;
    }
}
