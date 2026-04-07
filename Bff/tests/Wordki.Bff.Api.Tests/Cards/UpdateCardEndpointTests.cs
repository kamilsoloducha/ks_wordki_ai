using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Wordki.Bff.Api.Tests.Infrastructure;
using Wordki.Modules.Cards.Api.Requests;
using Wordki.Modules.Cards.Api.Responses;
using Wordki.Modules.Cards.Domain.Entities;

namespace Wordki.Bff.Api.Tests.Cards;

public sealed class UpdateCardEndpointTests(PostgresTestContainerFixture fixture)
    : IClassFixture<PostgresTestContainerFixture>
{
    [Fact]
    public async Task UpdateCard_WithValidPayload_ShouldReturnOkAndPersist()
    {
        var externalUserId = Guid.NewGuid();
        long cardId = 0;

        await fixture.ExecuteCardsDbContextAsync(async db =>
        {
            await db.Users.AddAsync(new User { ExternalUserId = externalUserId });
            await db.SaveChangesAsync();
            var user = await db.Users.SingleAsync(u => u.ExternalUserId == externalUserId);
            var group = new Group
            {
                Name = "G",
                FrontSideType = "EN",
                BackSideType = "PL",
                Type = GroupType.UserOwned,
                UserId = user.Id,
                Cards = []
            };
            await db.Groups.AddAsync(group);
            await db.SaveChangesAsync();

            var front = new CardSide { Label = "old-f", Example = "e1", Comment = "c1" };
            var back = new CardSide { Label = "old-b", Example = "e2", Comment = "c2" };
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
            cardId = card.Id;
            return 0;
        });

        var request = new UpdateCardRequest(
            UserId: externalUserId,
            Front: new CardSideDto("pear", "I eat a pear.", "fruit"),
            Back: new CardSideDto("gruszka", "Jem gruszkę.", "owoc"));

        var response = await fixture.Client.PatchAsJsonAsync($"/api/cards/{cardId}", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<CardDto>();
        payload.Should().NotBeNull();
        payload!.Id.Should().Be(cardId);
        payload.Front.Label.Should().Be("pear");
        payload.Back.Label.Should().Be("gruszka");

        var fromDb = await fixture.ExecuteCardsDbContextAsync(db =>
            db.Cards
                .AsNoTracking()
                .Include(c => c.FrontSide)
                .Include(c => c.BackSide)
                .SingleAsync(c => c.Id == cardId));

        fromDb.FrontSide.Label.Should().Be("pear");
        fromDb.BackSide.Label.Should().Be("gruszka");
    }

    [Fact]
    public async Task UpdateCard_ClearingOptionalFields_ShouldPersistEmptyStrings()
    {
        var externalUserId = Guid.NewGuid();
        long cardId = 0;

        await fixture.ExecuteCardsDbContextAsync(async db =>
        {
            await db.Users.AddAsync(new User { ExternalUserId = externalUserId });
            await db.SaveChangesAsync();
            var user = await db.Users.SingleAsync(u => u.ExternalUserId == externalUserId);
            var group = new Group
            {
                Name = "G",
                FrontSideType = "EN",
                BackSideType = "PL",
                Type = GroupType.UserOwned,
                UserId = user.Id,
                Cards = []
            };
            await db.Groups.AddAsync(group);
            await db.SaveChangesAsync();

            var front = new CardSide { Label = "a", Example = "long ex", Comment = "long com" };
            var back = new CardSide { Label = "b", Example = "x", Comment = "y" };
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
            cardId = card.Id;
            return 0;
        });

        var request = new UpdateCardRequest(
            UserId: externalUserId,
            Front: new CardSideDto("a", "", ""),
            Back: new CardSideDto("b", "", ""));

        var response = await fixture.Client.PatchAsJsonAsync($"/api/cards/{cardId}", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var fromDb = await fixture.ExecuteCardsDbContextAsync(db =>
            db.Cards
                .AsNoTracking()
                .Include(c => c.FrontSide)
                .Include(c => c.BackSide)
                .SingleAsync(c => c.Id == cardId));

        fromDb.FrontSide.Example.Should().BeEmpty();
        fromDb.FrontSide.Comment.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateCard_WithUnknownUser_ShouldReturnNotFound()
    {
        var request = new UpdateCardRequest(
            UserId: Guid.NewGuid(),
            Front: new CardSideDto("a", "", ""),
            Back: new CardSideDto("b", "", ""));

        var response = await fixture.Client.PatchAsJsonAsync("/api/cards/1", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var errorPayload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        errorPayload!.Errors.Should().Contain(x => x.Code == "cards.update_card.user.not_found");
    }

    [Fact]
    public async Task UpdateCard_WithUnknownCard_ShouldReturnNotFound()
    {
        var externalUserId = Guid.NewGuid();
        await fixture.ExecuteCardsDbContextAsync(async db =>
        {
            await db.Users.AddAsync(new User { ExternalUserId = externalUserId });
            await db.SaveChangesAsync();
            return 0;
        });

        var request = new UpdateCardRequest(
            UserId: externalUserId,
            Front: new CardSideDto("a", "", ""),
            Back: new CardSideDto("b", "", ""));

        var response = await fixture.Client.PatchAsJsonAsync("/api/cards/999999", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var errorPayload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        errorPayload!.Errors.Should().Contain(x => x.Code == "cards.update_card.not_found");
    }

    [Fact]
    public async Task UpdateCard_WhenNotOwner_ShouldReturnForbidden()
    {
        var ownerId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        long cardId = 0;

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

            var front = new CardSide { Label = "a", Example = "", Comment = "" };
            var back = new CardSide { Label = "b", Example = "", Comment = "" };
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
            cardId = card.Id;
            return 0;
        });

        var request = new UpdateCardRequest(
            UserId: otherId,
            Front: new CardSideDto("x", "", ""),
            Back: new CardSideDto("y", "", ""));

        var response = await fixture.Client.PatchAsJsonAsync($"/api/cards/{cardId}", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var errorPayload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        errorPayload!.Errors.Should().Contain(x => x.Code == "cards.update_card.forbidden");
    }

    private sealed record ErrorResponse(IReadOnlyList<ErrorItem> Errors);
    private sealed record ErrorItem(string Code, string Message, string? Field);
}
