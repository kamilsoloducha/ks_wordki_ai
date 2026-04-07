using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Wordki.Bff.Api.Tests.Infrastructure;
using Wordki.Modules.Cards.Api.Requests;
using Wordki.Modules.Cards.Api.Responses;
using Wordki.Modules.Cards.Domain.Entities;

namespace Wordki.Bff.Api.Tests.Cards;

public sealed class CreateCardGroupEndpointTests(PostgresTestContainerFixture fixture)
    : IClassFixture<PostgresTestContainerFixture>
{
    [Fact]
    public async Task CreateCardGroup_WithValidPayload_ShouldReturnCreated()
    {
        var externalUserId = Guid.NewGuid();
        await fixture.ExecuteCardsDbContextAsync(async db =>
        {
            await db.Users.AddAsync(new User { ExternalUserId = externalUserId });
            await db.SaveChangesAsync();
            return 0;
        });

        var cardsUserId = await fixture.ExecuteCardsDbContextAsync(db =>
            db.Users.Where(u => u.ExternalUserId == externalUserId).Select(u => u.Id).SingleAsync());

        var request = new CreateCardGroupRequest(
            UserId: externalUserId,
            Name: $"Group {Guid.NewGuid():N}",
            FrontSideType: "EN",
            BackSideType: "PL");

        var response = await fixture.Client.PostAsJsonAsync("/api/cards/groups", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var payload = await response.Content.ReadFromJsonAsync<CardGroupDto>();
        payload.Should().NotBeNull();
        payload!.Id.Should().BePositive();
        payload.Name.Should().Be(request.Name);
        payload.FrontSideType.Should().Be(request.FrontSideType);
        payload.BackSideType.Should().Be(request.BackSideType);

        var dbCheck = await fixture.ExecuteCardsDbContextAsync(async db =>
        {
            var created = await db.Groups.SingleOrDefaultAsync(x => x.Id == payload.Id);
            return created;
        });

        dbCheck.Should().NotBeNull();
        dbCheck!.Name.Should().Be(request.Name);
        dbCheck.FrontSideType.Should().Be(request.FrontSideType);
        dbCheck.BackSideType.Should().Be(request.BackSideType);
        dbCheck.Type.Should().Be(GroupType.UserOwned);
        dbCheck.UserId.Should().Be(cardsUserId);
    }

    [Fact]
    public async Task CreateCardGroup_WithUnknownUserId_ShouldReturnNotFound()
    {
        var request = new CreateCardGroupRequest(
            UserId: Guid.NewGuid(),
            Name: "My group",
            FrontSideType: "EN",
            BackSideType: "PL");

        var response = await fixture.Client.PostAsJsonAsync("/api/cards/groups", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var errorPayload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        errorPayload.Should().NotBeNull();
        errorPayload!.Errors.Should().Contain(x => x.Code == "cards.create_group.user.not_found");
    }

    [Fact]
    public async Task CreateCardGroup_WithInvalidPayload_ShouldReturnBadRequest()
    {
        var request = new CreateCardGroupRequest(
            UserId: Guid.Empty,
            Name: "",
            FrontSideType: "",
            BackSideType: new string('x', 150));

        var response = await fixture.Client.PostAsJsonAsync("/api/cards/groups", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorPayload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        errorPayload.Should().NotBeNull();
        errorPayload!.Errors.Should().NotBeEmpty();
        errorPayload.Errors.Should().Contain(x => x.Code == "cards.validation.user_id.required");
        errorPayload.Errors.Should().Contain(x => x.Code == "cards.validation.name.required");
        errorPayload.Errors.Should().Contain(x => x.Code == "cards.validation.front_side_type.required");
        errorPayload.Errors.Should().Contain(x => x.Code == "cards.validation.back_side_type.too_long");
    }

    private sealed record ErrorResponse(IReadOnlyList<ErrorItem> Errors);
    private sealed record ErrorItem(string Code, string Message, string? Field);
}
