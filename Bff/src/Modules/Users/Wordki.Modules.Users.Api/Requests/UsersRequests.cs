namespace Wordki.Modules.Users.Api.Requests;

public sealed record RegisterUserRequest(string Email, string Password, string UserName);

public sealed record ConfirmUserRequest(string Token);

public sealed record LoginUserRequest(string Email, string Password);

public sealed record ImpersonateUserRequest(Guid TargetUserId);
