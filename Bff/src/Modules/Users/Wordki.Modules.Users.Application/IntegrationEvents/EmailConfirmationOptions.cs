namespace Wordki.Modules.Users.Application.IntegrationEvents;

public sealed class EmailConfirmationOptions
{//
    public string ConfirmationUrlBase { get; set; } = "http://localhost:5173/api/users/confirm";
}
