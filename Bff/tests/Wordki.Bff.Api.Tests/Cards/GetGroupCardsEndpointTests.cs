using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Wordki.Bff.Api.Tests.Infrastructure;
using Wordki.Modules.Cards.Api.Responses;
using Wordki.Modules.Cards.Domain.Entities;

namespace Wordki.Bff.Api.Tests.Cards;

public sealed class GetGroupCardsEndpointTests(PostgresTestContainerFixture fixture)
    : IClassFixture<PostgresTestContainerFixture>
{
    [Fact]
    public async Task GetGroupCards_WithoutUserId_ShouldReturnBadRequest()
    {
        var response = await fixture.Client.GetAsync("/api/cards/groups/1/cards");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetGroupCards_WithUnknownUser_ShouldReturnNotFound()
    {
        var response = await fixture.Client.GetAsync(
            $"/api/cards/groups/1/cards?userId={Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var errorPayload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        errorPayload!.Errors.Should()
            .Contain(x => x.Code == "cards.get_group_cards.user.not_found");
    }

    [Fact]
    public async Task GetGroupCards_WithValidGroup_ShouldReturnCards()
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
                Name = "Vocab",
                FrontSideType = "EN",
                BackSideType = "PL",
                Type = GroupType.UserOwned,
                UserId = user.Id,
                Cards = []
            };
            await db.Groups.AddAsync(group);
            await db.SaveChangesAsync();
            groupId = group.Id;

            var front = new CardSide { Label = "hello", Example = "e.g.", Comment = "c1" };
            var back = new CardSide { Label = "cześć", Example = "", Comment = "" };
            await db.CardSides.AddRangeAsync(front, back);
            await db.SaveChangesAsync();

            var card = new Card
            {
                GroupId = group.Id,
                FrontSideId = front.Id,
                BackSideId = back.Id,
                FrontSide = front,
                BackSide = back
            };
            await db.Cards.AddAsync(card);
            await db.SaveChangesAsync();

            return 0;
        });

        var response = await fixture.Client.GetAsync(
            $"/api/cards/groups/{groupId}/cards?userId={externalUserId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<List<CardDto>>();
        payload.Should().NotBeNull();
        payload!.Should().ContainSingle();
        var c = payload[0];
        c.GroupId.Should().Be(groupId);
        c.Front.Label.Should().Be("hello");
        c.Back.Label.Should().Be("cześć");
    }

    [Fact]
    public async Task GetGroupCards_ForOtherUsersGroup_ShouldReturnForbidden()
    {
        var ownerId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        long groupId = 0;

        await fixture.ExecuteCardsDbContextAsync(async db =>
        {
            await db.Users.AddRangeAsync(
                new User { ExternalUserId = ownerId },
                new User { ExternalUserId = otherId });
            await db.SaveChangesAsync();

            var owner = await db.Users.SingleAsync(u => u.ExternalUserId == ownerId);

            var group = new Group
            {
                Name = "Private",
                FrontSideType = "A",
                BackSideType = "B",
                Type = GroupType.UserOwned,
                UserId = owner.Id,
                Cards = []
            };
            await db.Groups.AddAsync(group);
            await db.SaveChangesAsync();
            groupId = group.Id;

            return 0;
        });

        var response = await fixture.Client.GetAsync(
            $"/api/cards/groups/{groupId}/cards?userId={otherId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private sealed record ErrorResponse(IReadOnlyList<ErrorItem> Errors);
    private sealed record ErrorItem(string Code, string Message, string? Field);
}
