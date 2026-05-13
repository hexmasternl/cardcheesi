namespace CardCheesi.Auth;

public interface IJwtTokenService
{
    string GenerateAccessToken(Guid playerId, string playerName);
    (string RawToken, string Hash) GenerateRefreshToken();
    string ComputeSha256Hex(string input);
}
