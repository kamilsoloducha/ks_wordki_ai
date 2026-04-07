using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Wordki.Bff.SharedKernel.Events;
using Wordki.Modules.Lessons.Application.IntegrationEvents;

namespace Wordki.Modules.Lessons.Application;

public static class LessonsApplicationModule
{
    public static IServiceCollection AddLessonsApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(LessonsApplicationModule).Assembly));
        services.AddScoped<IOutboxMessageHandler, UserConfirmedEventHandler>();
        return services;
    }
}
