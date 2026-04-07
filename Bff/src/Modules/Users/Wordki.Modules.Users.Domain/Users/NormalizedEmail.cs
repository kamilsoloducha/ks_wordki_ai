namespace Wordki.Modules.Users.Domain.Users;

public sealed record NormalizedEmail
{
    public string Value { get; }

    private NormalizedEmail(string value)
    {
        Value = value;
    }

    public static NormalizedEmail Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email cannot be empty.", nameof(email));
        }

        return new NormalizedEmail(email.Trim().ToUpperInvariant());
    }

    public override string ToString() => Value;
}
