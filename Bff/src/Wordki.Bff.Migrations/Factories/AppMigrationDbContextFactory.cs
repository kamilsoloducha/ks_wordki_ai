using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Wordki.Bff.Migrations.Contexts;
using Wordki.Bff.Migrations.Infrastructure;

namespace Wordki.Bff.Migrations.Factories;

public sealed class AppMigrationDbContextFactory : IDesignTimeDbContextFactory<AppMigrationDbContext>
{
    public AppMigrationDbContext CreateDbContext(string[] args)
    {
        var connectionString = ConnectionStringResolver.Resolve("MigrationDatabase", "UsersDatabase");
        var optionsBuilder = new DbContextOptionsBuilder<AppMigrationDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        return new AppMigrationDbContext(optionsBuilder.Options);
    }
}
