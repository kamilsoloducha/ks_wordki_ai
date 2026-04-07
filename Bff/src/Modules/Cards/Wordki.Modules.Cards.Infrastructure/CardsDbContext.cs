using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Wordki.Modules.Cards.Application.Abstractions;
using Wordki.Modules.Cards.Domain.Entities;

namespace Wordki.Modules.Cards.Infrastructure;

internal sealed class CardsDbContext(
    DbContextOptions<CardsDbContext> options,
    IConfiguration configuration) : DbContext(options), ICardsDbContext
{
    private const string CardsConnectionStringName = "CardsDatabase";

    public DbSet<User> Users => Set<User>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<Card> Cards => Set<Card>();
    public DbSet<CardSide> CardSides => Set<CardSide>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured)
        {
            return;
        }

        var connectionString = configuration.GetConnectionString(CardsConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"Connection string '{CardsConnectionStringName}' is not configured.");
        }

        optionsBuilder.UseNpgsql(connectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CardsDbContext).Assembly);
    }
}
