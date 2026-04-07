using FluentAssertions;
using Wordki.Bff.Api.Tests.Infrastructure;

namespace Wordki.Bff.Api.Tests.Users;

public sealed class UsersHealthEndpointTests(PostgresTestContainerFixture fixture) : IClassFixture<PostgresTestContainerFixture>
{
    [Fact]
    public async Task GetUsersHealth_ShouldReturnOk()
    {
        var response = await fixture.Client.GetAsync("/api/users/health");

        response.IsSuccessStatusCode.Should().BeTrue();
    }
}
