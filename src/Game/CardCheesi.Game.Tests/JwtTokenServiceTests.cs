using CardCheesi.Auth;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace CardCheesi.Game.Tests;

public sealed class JwtTokenServiceTests
{
    private static JwtSettings ValidSettings => new()
    {
        SigningKey = "test-signing-key-that-is-at-least-32-bytes",
        Issuer = "test-issuer",
        Audience = "test-audience",
        AccessTokenExpiryMinutes = 10,
    };

    private static IJwtTokenService CreateService(JwtSettings? settings = null)
        => new JwtTokenService(Options.Create(settings ?? ValidSettings));

    [Fact]
    public void GenerateAccessToken_ValidSettings_ReturnsValidJwt()
    {
        var svc = CreateService();
        var playerId = Guid.NewGuid();
        const string playerName = "Alice";

        var token = svc.GenerateAccessToken(playerId, playerName);

        Assert.NotNull(token);
        Assert.NotEmpty(token);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        Assert.Equal(playerId.ToString(), jwt.Subject);
        Assert.Equal(ValidSettings.Issuer, jwt.Issuer);
    }

    [Fact]
    public void GenerateAccessToken_TokenValidatesWithCorrectKey()
    {
        var svc = CreateService();
        var playerId = Guid.NewGuid();
        var token = svc.GenerateAccessToken(playerId, "Bob");

        var handler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ValidSettings.SigningKey));

        var validationParams = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = ValidSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = ValidSettings.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.Zero,
        };

        var principal = handler.ValidateToken(token, validationParams, out _);
        Assert.NotNull(principal);
    }

    [Fact]
    public void GenerateAccessToken_TokenFailsWithWrongKey()
    {
        var svc = CreateService();
        var token = svc.GenerateAccessToken(Guid.NewGuid(), "Carol");

        var handler = new JwtSecurityTokenHandler();
        var wrongKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("wrong-key-but-still-32-bytes-long!"));

        var validationParams = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = wrongKey,
            ValidateIssuer = false,
            ValidateAudience = false,
        };

        Assert.Throws<SecurityTokenSignatureKeyNotFoundException>(() =>
            handler.ValidateToken(token, validationParams, out _));
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsDifferentTokensEachCall()
    {
        var svc = CreateService();
        var (raw1, hash1) = svc.GenerateRefreshToken();
        var (raw2, hash2) = svc.GenerateRefreshToken();

        Assert.NotEqual(raw1, raw2);
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void GenerateRefreshToken_HashDiffersFromRaw()
    {
        var svc = CreateService();
        var (raw, hash) = svc.GenerateRefreshToken();

        Assert.NotEqual(raw, hash);
    }

    [Fact]
    public void ComputeSha256Hex_SameInput_ReturnsSameHash()
    {
        var svc = CreateService();
        var hash1 = svc.ComputeSha256Hex("hello");
        var hash2 = svc.ComputeSha256Hex("hello");

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeSha256Hex_DifferentInputs_ReturnDifferentHashes()
    {
        var svc = CreateService();
        var hash1 = svc.ComputeSha256Hex("hello");
        var hash2 = svc.ComputeSha256Hex("world");

        Assert.NotEqual(hash1, hash2);
    }
}

public sealed class JwtSettingsValidatorTests
{
    [Fact]
    public void Validate_EmptySigningKey_ReturnsFailure()
    {
        var validator = new JwtSettingsValidator();
        var settings = new JwtSettings { SigningKey = string.Empty };

        var result = validator.Validate(null, settings);

        Assert.True(result.Failed);
    }

    [Fact]
    public void Validate_ShortSigningKey_ReturnsFailure()
    {
        var validator = new JwtSettingsValidator();
        var settings = new JwtSettings { SigningKey = "short" };

        var result = validator.Validate(null, settings);

        Assert.True(result.Failed);
    }

    [Fact]
    public void Validate_ValidSigningKey_ReturnsSuccess()
    {
        var validator = new JwtSettingsValidator();
        var settings = new JwtSettings { SigningKey = "this-is-a-valid-key-of-32-bytes!!" };

        var result = validator.Validate(null, settings);

        Assert.True(result.Succeeded);
    }
}
