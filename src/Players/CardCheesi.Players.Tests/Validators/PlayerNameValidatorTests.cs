using CardCheesi.Players.Validators;

namespace CardCheesi.Players.Tests.Validators;

public sealed class PlayerNameValidatorTests
{
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_EmptyOrNull_ReturnsError(string? name)
    {
        var errors = PlayerNameValidator.Validate(name);

        Assert.NotNull(errors);
        Assert.True(errors.ContainsKey("name"));
    }

    [Fact]
    public void Validate_WithLeadingWhitespace_ReturnsError()
    {
        var errors = PlayerNameValidator.Validate(" Alice");

        Assert.NotNull(errors);
        Assert.True(errors.ContainsKey("name"));
    }

    [Fact]
    public void Validate_WithTrailingWhitespace_ReturnsError()
    {
        var errors = PlayerNameValidator.Validate("Alice ");

        Assert.NotNull(errors);
        Assert.True(errors.ContainsKey("name"));
    }

    [Fact]
    public void Validate_NameExceeds50Chars_ReturnsError()
    {
        var errors = PlayerNameValidator.Validate(new string('x', 51));

        Assert.NotNull(errors);
        Assert.True(errors.ContainsKey("name"));
    }

    [Fact]
    public void Validate_NameWithControlCharacter_ReturnsError()
    {
        var errors = PlayerNameValidator.Validate("Alice\x01");

        Assert.NotNull(errors);
        Assert.True(errors.ContainsKey("name"));
    }

    [Theory]
    [InlineData("Alice")]
    [InlineData("Player 123")]
    [InlineData("Ünïcödé")]
    [InlineData("x")]
    public void Validate_ValidName_ReturnsNull(string name)
    {
        var errors = PlayerNameValidator.Validate(name);

        Assert.Null(errors);
    }

    [Fact]
    public void Validate_NameExactly50Chars_ReturnsNull()
    {
        var errors = PlayerNameValidator.Validate(new string('x', 50));

        Assert.Null(errors);
    }
}
