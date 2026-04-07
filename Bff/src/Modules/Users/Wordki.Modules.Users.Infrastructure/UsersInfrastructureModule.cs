using Microsoft.Extensions.DependencyInjection;
using Wordki.Bff.SharedKernel.Events;
using Wordki.Modules.Users.Application.Abstractions;
using Wordki.Modules.Users.Infrastructure.Persistence;
using Wordki.Modules.Users.Infrastructure.Security;

namespace Wordki.Modules.Users.Infrastructure;

public static class UsersInfrastructureModule
{
    public static IServiceCollection AddUsersInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<UsersDbContext>();
        services.AddOptions<BearerTokenOptions>()
            .BindConfiguration("Users:BearerToken");

        services.AddScoped<IUsersDbContext, UsersDbContextAdapter>();
        services.AddScoped<ISharedEventOutboxWriter, SharedEventOutboxWriter>();
        services.AddScoped<IOutboxMessageStore, UsersOutboxMessageStore>();
        services.AddSingleton<IPasswordHasher, Sha256PasswordHasher>();
        services.AddSingleton<IBearerTokenService, JwtBearerTokenService>();
        return services;
    }
}
