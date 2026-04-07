namespace Wordki.Modules.Users.Api.Responses;

public sealed record RegisterUserResponse(Guid UserId, string Email, string Status);

public sealed record ConfirmUserResponse(bool Confirmed, string Token);

public sealed record LoginUserResponse(string AccessToken, string TokenType, DateTime ExpiresAtUtc, CurrentUserDto User);

public sealed record ImpersonationResponse(Guid EffectiveUserId, string AccessToken, DateTime ExpiresAtUtc);

public sealed record CurrentUserDto(Guid Id, string Email, string Role, string Status);
