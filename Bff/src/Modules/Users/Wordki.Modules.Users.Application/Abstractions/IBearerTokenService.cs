namespace Wordki.Modules.Users.Application.Abstractions;

public interface IBearerTokenService
{
    string CreateToken(BearerTokenPayload payload);
    BearerTokenValidationResult ValidateToken(string token);
}

public sealed record BearerTokenPayload(Guid UserId, string Email, string Role);

public sealed record BearerTokenValidationResult(
    bool IsValid,
    Guid? UserId = null,
    string? Email = null,
    string? Role = null,
    string? Error = null);
