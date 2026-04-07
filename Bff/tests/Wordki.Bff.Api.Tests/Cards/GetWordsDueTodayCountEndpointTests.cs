using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Wordki.Bff.Api.Tests.Infrastructure;
using Wordki.Modules.Cards.Api.Responses;
using Wordki.Modules.Cards.Domain.Entities;

namespace Wordki.Bff.Api.Tests.Cards;

public sealed class GetWordsDueTodayCountEndpointTests(PostgresTestContainerFixture fixture)
    : IClassFixture<PostgresTestContainerFixture>
{
    [Fact]
    public async Task GetWordsDueTodayCount_WithoutUserId_ShouldReturnBadRequest()
    {
        var response = await fixture.Client.GetAsync("/api/cards/due-today-count");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetWordsDueTodayCount_WithUnknownUser_ShouldReturnNotFound()
    {
        var response = await fixture.Client.GetAsync(
            $"/api/cards/due-today-count?userId={Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var errorPayload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        errorPayload!.Errors.Should()
            .Contain(x => x.Code == "cards.get_due_today_count.user.not_found");
    }

    [Fact]
    public async Task GetWordsDueTodayCount_WithResultsDueToday_ShouldReturnDistinctCardCount()
    {
        var externalUserId = Guid.NewGuid();
        var utc = DateTime.UtcNow;
        var todayStart = new DateTime(utc.Year, utc.Month, utc.Day, 0, 0, 0, DateTimeKind.Utc);
        var tomorrowStart = todayStart.AddDays(1);

        await fixture.ExecuteCardsDbContextAsync(async db =>
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

            var f1 = new CardSide { Label = "a", Example = "", Comment = "" };
            var b1 = new CardSide { Label = "b", Example = "", Comment = "" };
            var f2 = new CardSide { Label = "c", Example = "", Comment = "" };
            var b2 = new CardSide { Label = "d", Example = "", Comment = "" };
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
                VALUES ({cu.Id}, {group.Id}, {f1.Id}, 0, {todayStart}, 0)
                """);
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO cards.results (user_id, group_id, card_side_id, drawer, next_repeat_utc, counter)
                VALUES ({cu.Id}, {group.Id}, {b1.Id}, 0, {tomorrowStart}, 0)
                """);
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO cards.results (user_id, group_id, card_side_id, drawer, next_repeat_utc, counter)
                VALUES ({cu.Id}, {group.Id}, {f2.Id}, 0, {todayStart}, 0)
                """);
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO cards.results (user_id, group_id, card_side_id, drawer, next_repeat_utc, counter)
                VALUES ({cu.Id}, {group.Id}, {b2.Id}, 0, {tomorrowStart}, 0)
                """);

            return 0;
        });

        var response = await fixture.Client.GetAsync(
            $"/api/cards/due-today-count?userId={externalUserId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<WordsDueTodayCountDto>();
        payload!.DueTodayCount.Should().Be(2);
    }

    [Fact]
    public async Task GetWordsDueTodayCount_WithOverdueRepeat_ShouldCountCard()
    {
        var externalUserId = Guid.NewGuid();
        var utc = DateTime.UtcNow;
        var todayStart = new DateTime(utc.Year, utc.Month, utc.Day, 0, 0, 0, DateTimeKind.Utc);
        var overdue = todayStart.AddDays(-3);

        await fixture.ExecuteCardsDbContextAsync(async db =>
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

            var f1 = new CardSide { Label = "a", Example = "", Comment = "" };
            var b1 = new CardSide { Label = "b", Example = "", Comment = "" };
            await db.CardSides.AddRangeAsync(f1, b1);
            await db.SaveChangesAsync();

            await db.Cards.AddAsync(
                new Card
                {
                    GroupId = group.Id,
                    FrontSideId = f1.Id,
                    BackSideId = b1.Id,
                    FrontSide = f1,
                    BackSide = b1
                });
            await db.SaveChangesAsync();

            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO cards.results (user_id, group_id, card_side_id, drawer, next_repeat_utc, counter)
                VALUES ({cu.Id}, {group.Id}, {f1.Id}, 0, {overdue}, 0)
                """);

            return 0;
        });

        var response = await fixture.Client.GetAsync(
            $"/api/cards/due-today-count?userId={externalUserId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<WordsDueTodayCountDto>();
        payload!.DueTodayCount.Should().Be(1);
    }

    [Fact]
    public async Task GetWordsDueTodayCount_OnlyQuestionSideType_ShouldReturnBadRequest()
    {
        var response = await fixture.Client.GetAsync(
            $"/api/cards/due-today-count?userId={Guid.NewGuid()}&questionSideType=EN");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorPayload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        errorPayload!.Errors.Should()
            .Contain(x => x.Code == "cards.validation.lesson_direction.partial");
    }

    [Fact]
    public async Task GetWordsDueTodayCount_WithDirectionFilter_ShouldCountOnlyMatchingGroups()
    {
        var externalUserId = Guid.NewGuid();
        var utc = DateTime.UtcNow;
        var todayStart = new DateTime(utc.Year, utc.Month, utc.Day, 0, 0, 0, DateTimeKind.Utc);
        var tomorrowStart = todayStart.AddDays(1);

        await fixture.ExecuteCardsDbContextAsync(async db =>
        {
            await db.Users.AddAsync(new User { ExternalUserId = externalUserId });
            await db.SaveChangesAsync();

            var cu = await db.Users.SingleAsync(u => u.ExternalUserId == externalUserId);

            var groupEnPl = new Group
            {
                Name = "EN-PL",
                FrontSideType = "EN",
                BackSideType = "PL",
                Type = GroupType.UserOwned,
                UserId = cu.Id,
                Cards = []
            };
            var groupDePl = new Group
            {
                Name = "DE-PL",
                FrontSideType = "DE",
                BackSideType = "PL",
                Type = GroupType.UserOwned,
                UserId = cu.Id,
                Cards = []
            };
            await db.Groups.AddRangeAsync(groupEnPl, groupDePl);
            await db.SaveChangesAsync();

            var f1 = new CardSide { Label = "a", Example = "", Comment = "" };
            var b1 = new CardSide { Label = "b", Example = "", Comment = "" };
            var f2 = new CardSide { Label = "c", Example = "", Comment = "" };
            var b2 = new CardSide { Label = "d", Example = "", Comment = "" };
            await db.CardSides.AddRangeAsync(f1, b1, f2, b2);
            await db.SaveChangesAsync();

            await db.Cards.AddRangeAsync(
                new Card
                {
                    GroupId = groupEnPl.Id,
                    FrontSideId = f1.Id,
                    BackSideId = b1.Id,
                    FrontSide = f1,
                    BackSide = b1
                },
                new Card
                {
                    GroupId = groupDePl.Id,
                    FrontSideId = f2.Id,
                    BackSideId = b2.Id,
                    FrontSide = f2,
                    BackSide = b2
                });
            await db.SaveChangesAsync();

            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO cards.results (user_id, group_id, card_side_id, drawer, next_repeat_utc, counter)
                VALUES ({cu.Id}, {groupEnPl.Id}, {f1.Id}, 0, {todayStart}, 0)
                """);
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO cards.results (user_id, group_id, card_side_id, drawer, next_repeat_utc, counter)
                VALUES ({cu.Id}, {groupDePl.Id}, {f2.Id}, 0, {todayStart}, 0)
                """);

            return 0;
        });

        var all = await fixture.Client.GetAsync(
            $"/api/cards/due-today-count?userId={externalUserId}");
        all.StatusCode.Should().Be(HttpStatusCode.OK);
        (await all.Content.ReadFromJsonAsync<WordsDueTodayCountDto>())!.DueTodayCount.Should().Be(2);

        var enPl = await fixture.Client.GetAsync(
            $"/api/cards/due-today-count?userId={externalUserId}&questionSideType=EN&answerSideType=PL");
        enPl.StatusCode.Should().Be(HttpStatusCode.OK);
        (await enPl.Content.ReadFromJsonAsync<WordsDueTodayCountDto>())!.DueTodayCount.Should().Be(1);

        var dePl = await fixture.Client.GetAsync(
            $"/api/cards/due-today-count?userId={externalUserId}&questionSideType=DE&answerSideType=PL");
        dePl.StatusCode.Should().Be(HttpStatusCode.OK);
        (await dePl.Content.ReadFromJsonAsync<WordsDueTodayCountDto>())!.DueTodayCount.Should().Be(1);

        var plEn = await fixture.Client.GetAsync(
            $"/api/cards/due-today-count?userId={externalUserId}&questionSideType=PL&answerSideType=EN");
        plEn.StatusCode.Should().Be(HttpStatusCode.OK);
        (await plEn.Content.ReadFromJsonAsync<WordsDueTodayCountDto>())!.DueTodayCount.Should().Be(1);
    }

    [Fact]
    public async Task GetWordsDueTodayCount_WithNoMatchingResults_ShouldReturnZero()
    {
        var externalUserId = Guid.NewGuid();

        await fixture.ExecuteCardsDbContextAsync(async db =>
        {
            await db.Users.AddAsync(new User { ExternalUserId = externalUserId });
            await db.SaveChangesAsync();
            return 0;
        });

        var response = await fixture.Client.GetAsync(
            $"/api/cards/due-today-count?userId={externalUserId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<WordsDueTodayCountDto>();
        payload!.DueTodayCount.Should().Be(0);
    }

    private sealed record ErrorResponse(IReadOnlyList<ErrorItem> Errors);
    private sealed record ErrorItem(string Code, string Message, string? Field);
}
