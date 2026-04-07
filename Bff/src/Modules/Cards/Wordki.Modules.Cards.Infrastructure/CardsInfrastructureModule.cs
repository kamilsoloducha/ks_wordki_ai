using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wordki.Modules.Cards.Application.Abstractions;

namespace Wordki.Modules.Cards.Infrastructure;

public static class CardsInfrastructureModule
{
    public static IServiceCollection AddCardsInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<CardsDbContext>();
        services.AddScoped<ICardsDbContext>(serviceProvider =>
            serviceProvider.GetRequiredService<CardsDbContext>());
        return services;
    }
}
