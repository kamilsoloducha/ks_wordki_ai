using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Wordki.Bff.Api.Tests.Infrastructure;
using Wordki.Modules.Cards.Api.Requests;
using Wordki.Modules.Cards.Api.Responses;
using Wordki.Modules.Cards.Domain.Entities;

namespace Wordki.Bff.Api.Tests.Cards;

public sealed class UpdateCardGroupEndpointTests(PostgresTestContainerFixture fixture)
    : IClassFixture<PostgresTestContainerFixture>
{
    [Fact]
    public async Task UpdateCardGroup_WithValidPayload_ShouldReturnOkAndPersist()
    {
        var externalUserId = Guid.NewGuid();
        long groupId = 0;

        await fixture.ExecuteCardsDbContextAsync(async db =>
        {
            await db.Users.AddAsync(new User { ExternalUserId = externalUserId });
            await db.SaveChangesAsync();
            var user = await db.Users.SingleAsync(u => u.ExternalUserId == externalUserId);
            var group = new Group
            {
                Name = "Old",
                FrontSideType = "DE",
                BackSideType = "FR",
                Type = GroupType.UserOwned,
                UserId = user.Id,
                Cards = []
            };
            await db.Groups.AddAsync(group);
            await db.SaveChangesAsync();
            groupId = group.Id;
            return 0;
        });

        var request = new UpdateCardGroupRequest(
            UserId: externalUserId,
            Name: "New name",
            FrontSideType: "EN",
            BackSideType: "PL");

        var response = await fixture.Client.PatchAsJsonAsync($"/api/cards/groups/{groupId}", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<CardGroupDto>();
        payload.Should().NotBeNull();
        payload!.Id.Should().Be(groupId);
        payload.Name.Should().Be("New name");
        payload.FrontSideType.Should().Be("EN");
        payload.BackSideType.Should().Be("PL");

        var fromDb = await fixture.ExecuteCardsDbContextAsync(db =>
            db.Groups.AsNoTracking().SingleAsync(g => g.Id == groupId));

        fromDb.Name.Should().Be("New name");
        fromDb.FrontSideType.Should().Be("EN");
        fromDb.BackSideType.Should().Be("PL");
    }

    [Fact]
    public async Task UpdateCardGroup_WithUnknownUser_ShouldReturnNotFound()
    {
        var request = new UpdateCardGroupRequest(
            UserId: Guid.NewGuid(),
            Name: "X",
            FrontSideType: "EN",
            BackSideType: "PL");

        var response = await fixture.Client.PatchAsJsonAsync("/api/cards/groups/1", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var errorPayload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        errorPayload!.Errors.Should().Contain(x => x.Code == "cards.update_group.user.not_found");
    }

    [Fact]
    public async Task UpdateCardGroup_WithUnknownGroup_ShouldReturnNotFound()
    {
        var externalUserId = Guid.NewGuid();
        await fixture.ExecuteCardsDbContextAsync(async db =>
        {
            await db.Users.AddAsync(new User { ExternalUserId = externalUserId });
            await db.SaveChangesAsync();
            return 0;
        });

        var request = new UpdateCardGroupRequest(
            UserId: externalUserId,
            Name: "X",
            FrontSideType: "EN",
            BackSideType: "PL");

        var response = await fixture.Client.PatchAsJsonAsync("/api/cards/groups/999999", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var errorPayload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        errorPayload!.Errors.Should().Contain(x => x.Code == "cards.update_group.not_found");
    }

    [Fact]
    public async Task UpdateCardGroup_WhenNotOwner_ShouldReturnForbidden()
    {
        var ownerId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        long groupId = 0;

        await fixture.ExecuteCardsDbContextAsync(async db =>
        {
            await db.Users.AddAsync(new User { ExternalUserId = ownerId });
            await db.Users.AddAsync(new User { ExternalUserId = otherId });
            await db.SaveChangesAsync();
            var owner = await db.Users.SingleAsync(u => u.ExternalUserId == ownerId);
            var group = new Group
            {
                Name = "G",
                FrontSideType = "EN",
                BackSideType = "PL",
                Type = GroupType.UserOwned,
                UserId = owner.Id,
                Cards = []
            };
            await db.Groups.AddAsync(group);
            await db.SaveChangesAsync();
            groupId = group.Id;
            return 0;
        });

        var request = new UpdateCardGroupRequest(
            UserId: otherId,
            Name: "Hacked",
            FrontSideType: "EN",
            BackSideType: "PL");

        var response = await fixture.Client.PatchAsJsonAsync($"/api/cards/groups/{groupId}", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var errorPayload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        errorPayload!.Errors.Should().Contain(x => x.Code == "cards.update_group.forbidden");
    }

    private sealed record ErrorResponse(IReadOnlyList<ErrorItem> Errors);
    private sealed record ErrorItem(string Code, string Message, string? Field);
}
