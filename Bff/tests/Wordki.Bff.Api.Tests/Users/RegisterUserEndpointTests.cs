using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Wordki.Bff.Api.Tests.Infrastructure;
using Wordki.Modules.Users.Domain.Users;
using Wordki.Modules.Users.Api.Requests;
using Wordki.Modules.Users.Api.Responses;

namespace Wordki.Bff.Api.Tests.Users;

public sealed class RegisterUserEndpointTests(PostgresTestContainerFixture fixture) : IClassFixture<PostgresTestContainerFixture>
{
    [Fact]
    public async Task RegisterUser_WithValidPayload_ShouldReturnCreated()
    {
        var request = new RegisterUserRequest(
            Email: $"john.{Guid.NewGuid():N}@example.com",
            Password: "Password123!",
            UserName: "john_doe");

        var response = await fixture.Client.PostAsJsonAsync("/api/users/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var payload = await response.Content.ReadFromJsonAsync<RegisterUserResponse>();
        payload.Should().NotBeNull();
        payload!.UserId.Should().NotBe(Guid.Empty);
        payload.Email.Should().Be(request.Email);
        payload.Status.Should().Be("PendingConfirmation");

        var dbCheck = await fixture.ExecuteDbContextAsync(async db =>
        {
            var createdUser = await db.Users.SingleOrDefaultAsync(x => x.Email == request.Email);
            return new { createdUser };
        });

        dbCheck.createdUser.Should().NotBeNull();
        dbCheck.createdUser!.Status.Should().Be(UserStatus.PendingConfirmation);
        dbCheck.createdUser.EmailConfirmationTokenHash.Should().NotBeNullOrWhiteSpace();
        dbCheck.createdUser.EmailConfirmationTokenExpiresAtUtc.Should().HaveValue();
    }

    [Fact]
    public async Task RegisterUser_WithInvalidPayload_ShouldReturnBadRequest()
    {
        var request = new RegisterUserRequest(
            Email: "invalid-email",
            Password: "123",
            UserName: "");

        var response = await fixture.Client.PostAsJsonAsync("/api/users/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        payload.Should().NotBeNull();
        payload!.Errors.Should().NotBeEmpty();
        payload.Errors.Should().Contain(x => x.Code == "users.validation.email.invalid");
        payload.Errors.Should().Contain(x => x.Code == "users.validation.password.too_short");
        payload.Errors.Should().Contain(x => x.Code == "users.validation.username.required");
    }

    [Fact]
    public async Task RegisterUser_WithDuplicateEmail_ShouldReturnConflict()
    {
        var email = $"duplicate.{Guid.NewGuid():N}@example.com";
        var firstRequest = new RegisterUserRequest(email, "Password123!", "first_user");
        var secondRequest = new RegisterUserRequest(email, "Password123!", "second_user");

        var firstResponse = await fixture.Client.PostAsJsonAsync("/api/users/register", firstRequest);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var secondResponse = await fixture.Client.PostAsJsonAsync("/api/users/register", secondRequest);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var payload = await secondResponse.Content.ReadFromJsonAsync<ErrorResponse>();
        payload.Should().NotBeNull();
        payload!.Errors.Should().Contain(x => x.Code == "users.register.email.already_exists");

        var usersCount = await fixture.ExecuteDbContextAsync(db =>
            db.Users.CountAsync(x => x.Email == email));
        usersCount.Should().Be(1);
    }

    private sealed record ErrorResponse(IReadOnlyList<ErrorItem> Errors);
    private sealed record ErrorItem(string Code, string Message, string? Field);
}
