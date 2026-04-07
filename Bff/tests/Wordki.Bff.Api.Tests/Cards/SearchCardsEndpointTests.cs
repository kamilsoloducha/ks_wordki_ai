using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Wordki.Bff.Api.Tests.Infrastructure;
using Wordki.Modules.Cards.Api.Responses;
using Wordki.Modules.Cards.Domain.Entities;

namespace Wordki.Bff.Api.Tests.Cards;

public sealed class SearchCardsEndpointTests(PostgresTestContainerFixture fixture)
    : IClassFixture<PostgresTestContainerFixture>
{
    private static string SearchUrl(
        Guid userId,
        int? drawer = null,
        long? groupId = null,
        bool getCount = false,
        bool getList = false,
        int? page = null,
        int? pageSize = null)
    {
        var parts = new List<string> { $"userId={Uri.EscapeDataString(userId.ToString())}" };
        if (drawer.HasValue)
        {
            parts.Add($"drawer={drawer.Value}");
        }

        if (groupId.HasValue)
        {
            parts.Add($"groupId={groupId.Value}");
        }

        if (getCount)
        {
            parts.Add("getCount=true");
        }

        if (getList)
        {
            parts.Add("getList=true");
        }

        if (page.HasValue)
        {
            parts.Add($"page={page.Value}");
        }

        if (pageSize.HasValue)
        {
            parts.Add($"pageSize={pageSize.Value}");
        }

        return "/api/cards/search?" + string.Join("&", parts);
    }

    [Fact]
    public async Task SearchCards_NoResponseFlags_ShouldReturnBadRequest()
    {
        var response = await fixture.Client.GetAsync(
            SearchUrl(Guid.NewGuid(), drawer: null, groupId: null, getCount: false, getList: false));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorPayload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        errorPayload!.Errors.Should().Contain(x => x.Code == "cards.validation.search.response.none");
    }

    [Fact]
    public async Task SearchCards_InvalidDrawer_ShouldReturnBadRequest()
    {
        var response = await fixture.Client.GetAsync(SearchUrl(Guid.NewGuid(), -1, getList: true));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SearchCards_InvalidPage_ShouldReturnBadRequest()
    {
        var response = await fixture.Client.GetAsync(
            SearchUrl(
                Guid.NewGuid(),
                drawer: null,
                groupId: null,
                getCount: false,
                getList: true,
                page: 0));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorPayload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        errorPayload!.Errors.Should().Contain(x => x.Code == "cards.validation.page.invalid");
    }

    [Fact]
    public async Task SearchCards_UnknownUser_ShouldReturnNotFound()
    {
        var response = await fixture.Client.GetAsync(SearchUrl(Guid.NewGuid(), 2, getList: true));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var errorPayload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        errorPayload!.Errors.Should().Contain(x => x.Code == "cards.search_cards.user.not_found");
    }

    [Fact]
    public async Task SearchCards_ByDrawer_ShouldReturnCardsWithAtLeastOneSideInDrawer()
    {
        var externalUserId = Guid.NewGuid();
        var utc = DateTime.UtcNow;

        var groupId = await fixture.ExecuteCardsDbContextAsync(async db =>
        {
            await db.Users.AddAsync(new User { ExternalUserId = externalUserId });
            await db.SaveChangesAsync();

            var cu = await db.Users.SingleAsync(u => u.ExternalUserId == externalUserId);

            var group = new Group
            {
                Name = "G",
                FrontSideType = "EN",
                BackSideType = "PL",
                Type = GroupType.UserOwned,
                UserId = cu.Id,
                Cards = []
            };
            await db.Groups.AddAsync(group);
            await db.SaveChangesAsync();

            var f1 = new CardSide { Label = "apple", Example = "", Comment = "" };
            var b1 = new CardSide { Label = "jabłko", Example = "", Comment = "" };
            var f2 = new CardSide { Label = "pear", Example = "", Comment = "" };
            var b2 = new CardSide { Label = "gruszka", Example = "", Comment = "" };
            await db.CardSides.AddRangeAsync(f1, b1, f2, b2);
            await db.SaveChangesAsync();

            await db.Cards.AddRangeAsync(
                new Card
                {
                    GroupId = group.Id,
                    FrontSideId = f1.Id,
                    BackSideId = b1.Id,
                    FrontSide = f1,
                    BackSide = b1
                },
                new Card
                {
                    GroupId = group.Id,
                    FrontSideId = f2.Id,
                    BackSideId = b2.Id,
                    FrontSide = f2,
                    BackSide = b2
                });
            await db.SaveChangesAsync();

            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO cards.results (user_id, group_id, card_side_id, drawer, next_repeat_utc, counter)
                VALUES ({cu.Id}, {group.Id}, {f1.Id}, 2, {utc}, 0)
                """);
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO cards.results (user_id, group_id, card_side_id, drawer, next_repeat_utc, counter)
                VALUES ({cu.Id}, {group.Id}, {b1.Id}, 5, {utc}, 0)
                """);
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO cards.results (user_id, group_id, card_side_id, drawer, next_repeat_utc, counter)
                VALUES ({cu.Id}, {group.Id}, {f2.Id}, 5, {utc}, 0)
                """);
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO cards.results (user_id, group_id, card_side_id, drawer, next_repeat_utc, counter)
                VALUES ({cu.Id}, {group.Id}, {b2.Id}, 5, {utc}, 0)
                """);

            return group.Id;
        });

        var resNoDrawer = await fixture.Client.GetAsync(SearchUrl(externalUserId, getList: true));
        resNoDrawer.StatusCode.Should().Be(HttpStatusCode.OK);
        var listNoDrawer = await resNoDrawer.Content.ReadFromJsonAsync<List<CardDto>>();
        listNoDrawer!.Should().HaveCount(2);

        var res2 = await fixture.Client.GetAsync(SearchUrl(externalUserId, 2, getList: true));
        res2.StatusCode.Should().Be(HttpStatusCode.OK);
        var list2 = await res2.Content.ReadFromJsonAsync<List<CardDto>>();
        list2.Should().NotBeNull();
        list2!.Should().HaveCount(1);
        list2[0].Front.Label.Should().Be("apple");

        var res5 = await fixture.Client.GetAsync(SearchUrl(externalUserId, 5, getList: true));
        res5.StatusCode.Should().Be(HttpStatusCode.OK);
        var list5 = await res5.Content.ReadFromJsonAsync<List<CardDto>>();
        list5!.Should().HaveCount(2);

        var res99 = await fixture.Client.GetAsync(SearchUrl(externalUserId, 99, getList: true));
        res99.StatusCode.Should().Be(HttpStatusCode.OK);
        var list99 = await res99.Content.ReadFromJsonAsync<List<CardDto>>();
        list99!.Should().BeEmpty();

        var resG = await fixture.Client.GetAsync(SearchUrl(externalUserId, 5, groupId, getList: true));
        resG.StatusCode.Should().Be(HttpStatusCode.OK);
        var listG = await resG.Content.ReadFromJsonAsync<List<CardDto>>();
        listG!.Should().HaveCount(2);

        var resCountOnly = await fixture.Client.GetAsync(SearchUrl(externalUserId, getCount: true));
        resCountOnly.StatusCode.Should().Be(HttpStatusCode.OK);
        var countDto = await resCountOnly.Content.ReadFromJsonAsync<SearchCardsCountDto>();
        countDto!.Count.Should().Be(2);

        var resBoth = await fixture.Client.GetAsync(SearchUrl(externalUserId, getCount: true, getList: true));
        resBoth.StatusCode.Should().Be(HttpStatusCode.OK);
        var both = await resBoth.Content.ReadFromJsonAsync<SearchCardsWithCountDto>();
        both!.Count.Should().Be(2);
        both.Items.Should().HaveCount(2);

        var resPage1 = await fixture.Client.GetAsync(
            SearchUrl(externalUserId, getList: true, page: 1, pageSize: 1));
        resPage1.StatusCode.Should().Be(HttpStatusCode.OK);
        var page1 = await resPage1.Content.ReadFromJsonAsync<List<CardDto>>();
        page1!.Should().HaveCount(1);

        var resBothPaged = await fixture.Client.GetAsync(
            SearchUrl(externalUserId, getCount: true, getList: true, page: 1, pageSize: 1));
        resBothPaged.StatusCode.Should().Be(HttpStatusCode.OK);
        var bothPaged = await resBothPaged.Content.ReadFromJsonAsync<SearchCardsWithCountDto>();
        bothPaged!.Count.Should().Be(2);
        bothPaged.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task SearchCards_ForeignGroup_ShouldReturnForbidden()
    {
        var ownerId = Guid.NewGuid();
        var otherId = Guid.NewGuid();

        var groupId = await fixture.ExecuteCardsDbContextAsync(async db =>
        {
            await db.Users.AddRangeAsync(
                new User { ExternalUserId = ownerId },
                new User { ExternalUserId = otherId });
            await db.SaveChangesAsync();

            var owner = await db.Users.SingleAsync(u => u.ExternalUserId == ownerId);

            var group = new Group
            {
                Name = "Mine",
                FrontSideType = "EN",
                BackSideType = "PL",
                Type = GroupType.UserOwned,
                UserId = owner.Id,
                Cards = []
            };
            await db.Groups.AddAsync(group);
            await db.SaveChangesAsync();

            return group.Id;
        });

        var response = await fixture.Client.GetAsync(SearchUrl(otherId, 0, groupId, getList: true));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var errorPayload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        errorPayload!.Errors.Should().Contain(x => x.Code == "cards.search_cards.group.forbidden");
    }

    private sealed record ErrorResponse(IReadOnlyList<ErrorItem> Errors);
    private sealed record ErrorItem(string Code, string Message, string? Field);
}
