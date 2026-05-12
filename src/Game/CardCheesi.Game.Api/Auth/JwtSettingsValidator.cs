using System.Text;
using Microsoft.Extensions.Options;

namespace CardCheesi.Game.Api.Auth;

public sealed class JwtSettingsValidator : IValidateOptions<JwtSettings>
{
    public ValidateOptionsResult Validate(string? name, JwtSettings options)
    {
        if (string.IsNullOrWhiteSpace(options.SigningKey))
            return ValidateOptionsResult.Fail(
                "Jwt__SigningKey is required. Set a signing key of at least 32 bytes (256 bits) " +
                "via environment variable or Aspire secrets.");

        if (Encoding.UTF8.GetByteCount(options.SigningKey) < 32)
            return ValidateOptionsResult.Fail(
                $"Jwt__SigningKey must be at least 32 bytes. " +
                $"The current value is {Encoding.UTF8.GetByteCount(options.SigningKey)} bytes. " +
                "Use a key of at least 32 UTF-8 characters (256 bits).");

        return ValidateOptionsResult.Success;
    }
}
