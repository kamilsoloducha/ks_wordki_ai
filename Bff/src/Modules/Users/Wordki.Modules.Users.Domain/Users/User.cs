namespace Wordki.Modules.Users.Domain.Users;

public sealed class User
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public required NormalizedEmail NormalizedEmail { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string PasswordHash { get; init; } = string.Empty;
    public UserRole Role { get; init; } = UserRole.User;
    public UserStatus Status { get; set; } = UserStatus.PendingConfirmation;
    public DateTime? EmailConfirmedAtUtc { get; set; }
    public string? EmailConfirmationTokenHash { get; set; }
    public DateTime? EmailConfirmationTokenExpiresAtUtc { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockoutEndsAtUtc { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");
}

public enum UserStatus
{
    PendingConfirmation = 1,
    Active = 2,
    Deleted = 3,
    Blocked = 4
}
