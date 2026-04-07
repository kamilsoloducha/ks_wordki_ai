using Microsoft.EntityFrameworkCore;
using Wordki.Bff.SharedKernel.Events;
using Wordki.Modules.Cards.Domain.Entities;
using Wordki.Modules.Cards.Infrastructure;
using Wordki.Modules.Lessons.Domain.Entities;
using Wordki.Modules.Lessons.Infrastructure;
using Wordki.Modules.Users.Infrastructure;
using UsersUser = Wordki.Modules.Users.Domain.Users.User;
using CardsUser = Wordki.Modules.Cards.Domain.Entities.User;
using LessonsUser = Wordki.Modules.Lessons.Domain.Entities.User;

namespace Wordki.Bff.Migrations.Contexts;

public sealed class AppMigrationDbContext(DbContextOptions<AppMigrationDbContext> options) : DbContext(options)
{
    public DbSet<UsersUser> Users => Set<UsersUser>();
    public DbSet<SharedEventMessage> SharedEventMessages => Set<SharedEventMessage>();
    public DbSet<CardsUser> CardsUsers => Set<CardsUser>();
    public DbSet<Group> CardGroups => Set<Group>();
    public DbSet<Card> Cards => Set<Card>();
    public DbSet<CardSide> CardSides => Set<CardSide>();
    public DbSet<Result> CardResults => Set<Result>();

    public DbSet<LessonsUser> LessonsUsers => Set<LessonsUser>();
    public DbSet<Lesson> LessonsLessons => Set<Lesson>();
    public DbSet<LessonRepetition> LessonsRepetitions => Set<LessonRepetition>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UsersInfrastructureModule).Assembly);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CardsInfrastructureModule).Assembly);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LessonsInfrastructureModule).Assembly);
    }
}
