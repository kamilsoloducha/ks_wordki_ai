using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Wordki.Modules.Cards.Infrastructure;
using Wordki.Modules.Users.Infrastructure;

namespace Wordki.Bff.Api.Tests.Infrastructure;

public sealed class PostgresTestContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("wordki_tests")
        .WithUsername("wordki")
        .WithPassword("wordki")
        .Build();

    private TestApiFactory? _factory;

    public HttpClient Client { get; private set; } = default!;

    public string ConnectionString => _postgresContainer.GetConnectionString();

    internal async Task<TResult> ExecuteDbContextAsync<TResult>(Func<UsersDbContext, Task<TResult>> action)
    {
        if (_factory is null)
        {
            throw new InvalidOperationException("Test factory is not initialized.");
        }

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
        return await action(dbContext);
    }

    internal async Task<TResult> ExecuteCardsDbContextAsync<TResult>(Func<CardsDbContext, Task<TResult>> action)
    {
        if (_factory is null)
        {
            throw new InvalidOperationException("Test factory is not initialized.");
        }

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CardsDbContext>();
        return await action(dbContext);
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        _factory = new TestApiFactory(ConnectionString);

        using (var scope = _factory.Services.CreateScope())
        {
            var usersDbContext = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
            await usersDbContext.Database.EnsureCreatedAsync();

            // EnsureCreated only runs when the database has no tables; a second context on the same DB is skipped.
            var cardsDbContext = scope.ServiceProvider.GetRequiredService<CardsDbContext>();
            var cardsCreator = cardsDbContext.GetService<IRelationalDatabaseCreator>()
                ?? throw new InvalidOperationException("IRelationalDatabaseCreator is not available for CardsDbContext.");
            cardsCreator.CreateTables();
        }

        Client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();

        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await _postgresContainer.DisposeAsync();
    }
}
