using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Wordki.Modules.Users.Application.Abstractions;
using Wordki.Modules.Users.Application.IntegrationEvents;
using Wordki.Modules.Users.Application.Security;

namespace Wordki.Modules.Users.Application;

public static class UsersApplicationModule
{
    public static IServiceCollection AddUsersApplication(this IServiceCollection services)
    {
        services.AddOptions<EmailConfirmationOptions>()
            .BindConfiguration("Users:EmailConfirmation");
        services.AddSingleton<IConfirmationTokenHasher, Sha256ConfirmationTokenHasher>();
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(UsersApplicationModule).Assembly));

        return services;
    }
}
