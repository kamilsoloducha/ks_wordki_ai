using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Wordki.Bff.SharedKernel.Events;
using Wordki.Modules.Cards.Application.IntegrationEvents;

namespace Wordki.Modules.Cards.Application;

public static class CardsApplicationModule
{
    public static IServiceCollection AddCardsApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CardsApplicationModule).Assembly));
        services.AddScoped<IOutboxMessageHandler, UserConfirmedEventHandler>();
        services.AddScoped<IOutboxMessageHandler, RepeatAddedEventHandler>();
        return services;
    }
}
