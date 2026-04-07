using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Wordki.Bff.Api.Tests.Infrastructure;
using Wordki.Modules.Cards.Api.Responses;
using Wordki.Modules.Cards.Domain.Entities;

namespace Wordki.Bff.Api.Tests.Cards;

public sealed class GetDistinctSideTypePairsEndpointTests(PostgresTestContainerFixture fixture)
    : IClassFixture<PostgresTestContainerFixture>
{
    [Fact]
    public async Task GetSideTypePairs_WithoutUserId_ShouldReturnBadRequest()
    {
        var response = await fixture.Client.GetAsync("/api/cards/side-type-pairs");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetSideTypePairs_UnknownUser_ShouldReturnNotFound()
    {
        var response = await fixture.Client.GetAsync(
            $"/api/cards/side-type-pairs?userId={Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var errorPayload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        errorPayload!.Errors.Should().Contain(x => x.Code == "cards.get_side_type_pairs.user.not_found");
    }

    [Fact]
    public async Task GetSideTypePairs_ShouldReturnUniquePairsIgnoringDirection()
    {
        var externalUserId = Guid.NewGuid();

        await fixture.ExecuteCardsDbContextAsync(async db =>
        {
            await db.Users.AddAsync(new User { ExternalUserId = externalUserId });
            await db.SaveChangesAsync();

            var user = await db.Users.SingleAsync(u => u.ExternalUserId == externalUserId);

            await db.Groups.AddRangeAsync(
                new Group
                {
                    Name = "A",
                    FrontSideType = "EN",
                    BackSideType = "PL",
                    Type = GroupType.UserOwned,
                    UserId = user.Id,
                    Cards = []
                },
                new Group
                {
                    Name = "B",
                    FrontSideType = "PL",
                    BackSideType = "EN",
                    Type = GroupType.UserOwned,
                    UserId = user.Id,
                    Cards = []
                },
                new Group
                {
                    Name = "C",
                    FrontSideType = "DE",
                    BackSideType = "PL",
                    Type = GroupType.UserOwned,
                    UserId = user.Id,
                    Cards = []
                });
            await db.SaveChangesAsync();

            return 0;
        });

        var response = await fixture.Client.GetAsync(
            $"/api/cards/side-type-pairs?userId={externalUserId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await response.Content.ReadFromJsonAsync<List<SideTypePairDto>>();
        list.Should().NotBeNull();
        list!.Should().HaveCount(2);
        list.Should().Contain(x => x.SideType1 == "DE" && x.SideType2 == "PL");
        list.Should().Contain(x => x.SideType1 == "EN" && x.SideType2 == "PL");
    }

    [Fact]
    public async Task GetSideTypePairs_NoGroups_ShouldReturnEmptyList()
    {
        var externalUserId = Guid.NewGuid();

        await fixture.ExecuteCardsDbContextAsync(async db =>
        {
            await db.Users.AddAsync(new User { ExternalUserId = externalUserId });
            await db.SaveChangesAsync();

            return 0;
        });

        var response = await fixture.Client.GetAsync(
            $"/api/cards/side-type-pairs?userId={externalUserId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await response.Content.ReadFromJsonAsync<List<SideTypePairDto>>();
        list.Should().NotBeNull();
        list!.Should().BeEmpty();
    }

    private sealed record ErrorResponse(IReadOnlyList<ErrorItem> Errors);
    private sealed record ErrorItem(string Code, string Message, string? Field);
}
