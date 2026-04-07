using Microsoft.Extensions.Configuration;

namespace Wordki.Bff.Migrations.Infrastructure;

internal static class ConnectionStringResolver
{
    public static string Resolve(params string[] connectionStringNames)
    {
        if (connectionStringNames.Length == 0)
        {
            throw new ArgumentException("At least one connection string name must be provided.", nameof(connectionStringNames));
        }

        var currentDirectory = Directory.GetCurrentDirectory();
        var apiDirectory = Path.Combine(currentDirectory, "src", "Wordki.Bff.Api");
        var configDirectory = Directory.Exists(apiDirectory) ? apiDirectory : currentDirectory;

        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(configDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        foreach (var connectionStringName in connectionStringNames)
        {
            var connectionString = configuration.GetConnectionString(connectionStringName);
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                return connectionString;
            }
        }

        throw new InvalidOperationException(
            $"None of connection strings '{string.Join(", ", connectionStringNames)}' is configured in '{configDirectory}'.");
    }
}
