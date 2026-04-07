using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Wordki.Modules.Lessons.Application.Abstractions;
using Wordki.Modules.Lessons.Domain.Entities;

namespace Wordki.Modules.Lessons.Infrastructure;

internal sealed class LessonsDbContext(
    DbContextOptions<LessonsDbContext> options,
    IConfiguration configuration) : DbContext(options), ILessonsDbContext
{
    private const string LessonsConnectionStringName = "LessonsDatabase";

    public DbSet<User> Users => Set<User>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<LessonRepetition> LessonRepetitions => Set<LessonRepetition>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured)
        {
            return;
        }

        var connectionString = configuration.GetConnectionString(LessonsConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"Connection string '{LessonsConnectionStringName}' is not configured.");
        }

        optionsBuilder.UseNpgsql(connectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LessonsDbContext).Assembly);
    }
}
