using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wordki.Modules.Lessons.Application.Abstractions;

namespace Wordki.Modules.Lessons.Infrastructure;

public static class LessonsInfrastructureModule
{
    public static IServiceCollection AddLessonsInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<LessonsDbContext>();
        services.AddScoped<ILessonsDbContext>(serviceProvider =>
            serviceProvider.GetRequiredService<LessonsDbContext>());
        return services;
    }
}
