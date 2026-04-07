using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Wordki.Bff.Api.Tests.Infrastructure;
using Wordki.Modules.Users.Api.Requests;
using Wordki.Modules.Users.Api.Responses;
using Wordki.Modules.Users.Domain.Users;

namespace Wordki.Bff.Api.Tests.Users;

public sealed class ConfirmUserEndpointTests(PostgresTestContainerFixture fixture) : IClassFixture<PostgresTestContainerFixture>
{
    [Fact]
    public async Task ConfirmUser_WithValidToken_ShouldReturnOkAndActivateUser()
    {
        var registerRequest = new RegisterUserRequest(
            Email: $"confirm.{Guid.NewGuid():N}@example.com",
            Password: "Password123!",
            UserName: "confirm_user");

        var registerResponse = await fixture.Client.PostAsJsonAsync("/api/users/register", registerRequest);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var registerPayload = await registerResponse.Content.ReadFromJsonAsync<RegisterUserResponse>();
        registerPayload.Should().NotBeNull();

        var confirmationToken = Guid.NewGuid().ToString("N");
        var confirmationTokenHash = HashToken(confirmationToken);

        var dbBeforeConfirm = await fixture.ExecuteDbContextAsync(async db =>
        {
            var user = await db.Users.SingleAsync(x => x.Email == registerRequest.Email);
            user.EmailConfirmationTokenHash = confirmationTokenHash;
            user.EmailConfirmationTokenExpiresAtUtc = DateTime.UtcNow.AddHours(1);
            await db.SaveChangesAsync();

            return new
            {
                userId = user.Id
            };
        });

        var confirmResponse = await fixture.Client.PostAsJsonAsync(
            "/api/users/confirm",
            new ConfirmUserRequest(confirmationToken));

        confirmResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var confirmPayload = await confirmResponse.Content.ReadFromJsonAsync<ConfirmUserResponse>();
        confirmPayload.Should().NotBeNull();
        confirmPayload!.Confirmed.Should().BeTrue();
        confirmPayload.Token.Should().Be(confirmationToken);

        var dbAfterConfirm = await fixture.ExecuteDbContextAsync(async db =>
        {
            var user = await db.Users.SingleAsync(x => x.Id == dbBeforeConfirm.userId);
            var userConfirmedEvents = await db.SharedEventMessages
                .Where(x => x.PublisherName == "Users" &&
                            x.DataType == "UserConfirmed" &&
                            x.Payload.Contains(dbBeforeConfirm.userId.ToString()))
                .ToListAsync();

            return new { user, userConfirmedEvents };
        });

        dbAfterConfirm.user.Status.Should().Be(UserStatus.Active);
        dbAfterConfirm.user.EmailConfirmedAtUtc.Should().HaveValue();
        dbAfterConfirm.user.EmailConfirmationTokenHash.Should().BeNull();
        dbAfterConfirm.user.EmailConfirmationTokenExpiresAtUtc.Should().BeNull();
        dbAfterConfirm.userConfirmedEvents.Should().HaveCount(1);
        dbAfterConfirm.userConfirmedEvents.Single().ConsumerName.Should().Be("*");
        dbAfterConfirm.userConfirmedEvents.Single().DataType.Should().Be("UserConfirmed");
    }

    [Fact]
    public async Task ConfirmUser_WithEmptyToken_ShouldReturnBadRequest()
    {
        var response = await fixture.Client.PostAsJsonAsync("/api/users/confirm", new ConfirmUserRequest(" "));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        payload.Should().NotBeNull();
        payload!.Errors.Should().Contain(x => x.Code == "users.validation.token.required");
    }

    [Fact]
    public async Task ConfirmUser_WithInvalidToken_ShouldReturnNotFound()
    {
        var response = await fixture.Client.PostAsJsonAsync(
            "/api/users/confirm",
            new ConfirmUserRequest(Guid.NewGuid().ToString("N")));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        payload.Should().NotBeNull();
        payload!.Errors.Should().Contain(x => x.Code == "users.confirm.token.invalid");
    }

    [Fact]
    public async Task ConfirmUser_GetEndpoint_WithInvalidToken_ShouldReturnNotFound()
    {
        var token = Guid.NewGuid().ToString("N");
        var response = await fixture.Client.GetAsync($"/api/users/confirm?token={token}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        payload.Should().NotBeNull();
        payload!.Errors.Should().Contain(x => x.Code == "users.confirm.token.invalid");
    }

    private static string HashToken(string token)
    {
        var tokenBytes = Encoding.UTF8.GetBytes(token);
        var hashBytes = SHA256.HashData(tokenBytes);
        return Convert.ToHexString(hashBytes);
    }

    private sealed record ErrorResponse(IReadOnlyList<ErrorItem> Errors);
    private sealed record ErrorItem(string Code, string Message, string? Field);
}
