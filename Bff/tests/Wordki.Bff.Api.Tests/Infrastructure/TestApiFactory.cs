using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Wordki.Bff.Api.Tests.Infrastructure;

public sealed class TestApiFactory(string usersConnectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            var inMemoryConfig = new Dictionary<string, string?>
            {
                ["ConnectionStrings:UsersDatabase"] = usersConnectionString,
                ["ConnectionStrings:CardsDatabase"] = usersConnectionString,
                ["Users:BearerToken:Issuer"] = "wordki.tests",
                ["Users:BearerToken:Audience"] = "wordki.tests.clients",
                ["Users:BearerToken:SecretKey"] = "TEST_ONLY_CHANGE_ME_TO_A_LONG_RANDOM_SECRET_KEY",
                ["Users:BearerToken:ExpirationMinutes"] = "60",
                ["Users:EmailConfirmation:ConfirmationUrlBase"] = "http://localhost:5173/confirm-email"
            };

            configBuilder.AddInMemoryCollection(inMemoryConfig);
        });
    }
}
