namespace Wordki.Modules.Users.Infrastructure.Security;

public sealed class BearerTokenOptions
{
    public string Issuer { get; set; } = "wordki.bff";
    public string Audience { get; set; } = "wordki.clients";
    public string SecretKey { get; set; } = "CHANGE_ME_TO_A_LONG_RANDOM_SECRET_KEY";
    public int ExpirationMinutes { get; set; } = 60;
}
