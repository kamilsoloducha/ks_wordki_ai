using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Wordki.Bff.SharedKernel.Events;
using Wordki.Modules.Users.Domain.Users;

namespace Wordki.Modules.Users.Infrastructure;

internal sealed class UsersDbContext(
    DbContextOptions<UsersDbContext> options,
    IConfiguration configuration) : DbContext(options)
{
    private const string UsersConnectionStringName = "UsersDatabase";

    public DbSet<User> Users => Set<User>();
    public DbSet<SharedEventMessage> SharedEventMessages => Set<SharedEventMessage>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured)
        {
            return;
        }

        var connectionString = configuration.GetConnectionString(UsersConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"Connection string '{UsersConnectionStringName}' is not configured.");
        }

        optionsBuilder.UseNpgsql(connectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UsersDbContext).Assembly);
    }
}
