using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Wordki.Bff.Api.Tests.Infrastructure;
using Wordki.Modules.Cards.Api.Responses;
using Wordki.Modules.Cards.Domain.Entities;

namespace Wordki.Bff.Api.Tests.Cards;

public sealed class GetUserCardGroupsEndpointTests(PostgresTestContainerFixture fixture)
    : IClassFixture<PostgresTestContainerFixture>
{
    [Fact]
    public async Task GetUserCardGroups_WithoutUserId_ShouldReturnBadRequest()
    {
        var response = await fixture.Client.GetAsync("/api/cards/groups");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetUserCardGroups_WithUnknownUser_ShouldReturnNotFound()
    {
        var response = await fixture.Client.GetAsync($"/api/cards/groups?userId={Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var errorPayload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        errorPayload!.Errors.Should().Contain(x => x.Code == "cards.get_groups.user.not_found");
    }

    [Fact]
    public async Task GetUserCardGroups_WithValidUser_ShouldReturnGroupsWithCardCounts()
    {
        var externalUserId = Guid.NewGuid();

        await fixture.ExecuteCardsDbContextAsync(async db =>
        {
            await db.Users.AddAsync(new User { ExternalUserId = externalUserId });
            await db.SaveChangesAsync();

            var user = await db.Users.SingleAsync(u => u.ExternalUserId == externalUserId);

            var groupA = new Group
            {
                Name = "Alpha",
                FrontSideType = "EN",
                BackSideType = "PL",
                Type = GroupType.UserOwned,
                UserId = user.Id,
                Cards = []
            };
            var groupB = new Group
            {
                Name = "Beta",
                FrontSideType = "DE",
                BackSideType = "PL",
                Type = GroupType.UserOwned,
                UserId = user.Id,
                Cards = []
            };
            await db.Groups.AddRangeAsync(groupA, groupB);
            await db.SaveChangesAsync();

            var front1 = new CardSide { Label = "a", Example = "", Comment = "" };
            var back1 = new CardSide { Label = "b", Example = "", Comment = "" };
            await db.CardSides.AddRangeAsync(front1, back1);
            await db.SaveChangesAsync();

            var card = new Card
            {
                GroupId = groupA.Id,
                FrontSideId = front1.Id,
                BackSideId = back1.Id,
                FrontSide = front1,
                BackSide = back1
            };
            await db.Cards.AddAsync(card);
            await db.SaveChangesAsync();

            return 0;
        });

        var response = await fixture.Client.GetAsync($"/api/cards/groups?userId={externalUserId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<List<UserCardGroupDto>>();
        payload.Should().NotBeNull();
        payload!.Should().HaveCount(2);
        payload.Should().BeInAscendingOrder(x => x.Id);

        var alpha = payload.Single(x => x.Name == "Alpha");
        alpha.FrontSideType.Should().Be("EN");
        alpha.BackSideType.Should().Be("PL");
        alpha.CardCount.Should().Be(1);

        var beta = payload.Single(x => x.Name == "Beta");
        beta.FrontSideType.Should().Be("DE");
        beta.BackSideType.Should().Be("PL");
        beta.CardCount.Should().Be(0);
    }

    [Fact]
    public async Task GetUserCardGroups_WithValidUserAndNoGroups_ShouldReturnEmptyList()
    {
        var externalUserId = Guid.NewGuid();
        await fixture.ExecuteCardsDbContextAsync(async db =>
        {
            await db.Users.AddAsync(new User { ExternalUserId = externalUserId });
            await db.SaveChangesAsync();
            return 0;
        });

        var response = await fixture.Client.GetAsync($"/api/cards/groups?userId={externalUserId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<List<UserCardGroupDto>>();
        payload.Should().NotBeNull();
        payload!.Should().BeEmpty();
    }

    private sealed record ErrorResponse(IReadOnlyList<ErrorItem> Errors);
    private sealed record ErrorItem(string Code, string Message, string? Field);
}
