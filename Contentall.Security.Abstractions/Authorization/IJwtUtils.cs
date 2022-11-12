namespace Contentall.Security.Abstractions.Authorization;

using Contentall.Security.Abstractions.Entities;

public interface IJwtUtils
{
    public string GenerateJwtToken(Account account);
    public string ValidateJwtToken(string token);
    public RefreshToken GenerateRefreshToken(string ipAddress);
}
