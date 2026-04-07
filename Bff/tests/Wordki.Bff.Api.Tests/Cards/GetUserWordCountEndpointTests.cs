using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Wordki.Bff.Api.Tests.Infrastructure;
using Wordki.Modules.Cards.Api.Responses;
using Wordki.Modules.Cards.Domain.Entities;

namespace Wordki.Bff.Api.Tests.Cards;

public sealed class GetUserWordCountEndpointTests(PostgresTestContainerFixture fixture)
    : IClassFixture<PostgresTestContainerFixture>
{
    [Fact]
    public async Task GetUserWordCount_WithoutUserId_ShouldReturnBadRequest()
    {
        var response = await fixture.Client.GetAsync("/api/cards/words-count");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetUserWordCount_WithUnknownUser_ShouldReturnNotFound()
    {
        var response = await fixture.Client.GetAsync(
            $"/api/cards/words-count?userId={Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var errorPayload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        errorPayload!.Errors.Should().Contain(x => x.Code == "cards.get_word_count.user.not_found");
    }

    [Fact]
    public async Task GetUserWordCount_WithValidUser_ShouldReturnTotalCardCountAcrossGroups()
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

            var f1 = new CardSide { Label = "a", Example = "", Comment = "" };
            var b1 = new CardSide { Label = "b", Example = "", Comment = "" };
            var f2 = new CardSide { Label = "c", Example = "", Comment = "" };
            var b2 = new CardSide { Label = "d", Example = "", Comment = "" };
            await db.CardSides.AddRangeAsync(f1, b1, f2, b2);
            await db.SaveChangesAsync();

            await db.Cards.AddAsync(new Card
            {
                GroupId = groupA.Id,
                FrontSideId = f1.Id,
                BackSideId = b1.Id,
                FrontSide = f1,
                BackSide = b1
            });
            await db.Cards.AddAsync(new Card
            {
                GroupId = groupB.Id,
                FrontSideId = f2.Id,
                BackSideId = b2.Id,
                FrontSide = f2,
                BackSide = b2
            });
            await db.SaveChangesAsync();

            return 0;
        });

        var response = await fixture.Client.GetAsync(
            $"/api/cards/words-count?userId={externalUserId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<UserWordCountDto>();
        payload.Should().NotBeNull();
        payload!.WordCount.Should().Be(2);
    }

    [Fact]
    public async Task GetUserWordCount_WithValidUserAndNoCards_ShouldReturnZero()
    {
        var externalUserId = Guid.NewGuid();
        await fixture.ExecuteCardsDbContextAsync(async db =>
        {
            await db.Users.AddAsync(new User { ExternalUserId = externalUserId });
            await db.SaveChangesAsync();
            return 0;
        });

        var response = await fixture.Client.GetAsync(
            $"/api/cards/words-count?userId={externalUserId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<UserWordCountDto>();
        payload!.WordCount.Should().Be(0);
    }

    private sealed record ErrorResponse(IReadOnlyList<ErrorItem> Errors);
    private sealed record ErrorItem(string Code, string Message, string? Field);
}
