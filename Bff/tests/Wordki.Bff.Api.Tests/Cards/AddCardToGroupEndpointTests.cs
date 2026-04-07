using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Wordki.Bff.Api.Tests.Infrastructure;
using Wordki.Modules.Cards.Api.Requests;
using Wordki.Modules.Cards.Api.Responses;
using Wordki.Modules.Cards.Domain.Entities;

namespace Wordki.Bff.Api.Tests.Cards;

public sealed class AddCardToGroupEndpointTests(PostgresTestContainerFixture fixture)
    : IClassFixture<PostgresTestContainerFixture>
{
    [Fact]
    public async Task AddCardToGroup_WithRequiredFieldsOnly_ShouldReturnCreated()
    {
        var (externalUserId, groupId) = await SeedUserAndGroupAsync();

        var request = new AddCardToGroupRequest(
            UserId: externalUserId,
            GroupId: groupId,
            FrontLabel: "hello",
            BackLabel: "czesc");

        var response = await fixture.Client.PostAsJsonAsync("/api/cards/cards", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var payload = await response.Content.ReadFromJsonAsync<CardDto>();
        payload.Should().NotBeNull();
        payload!.Id.Should().BePositive();
        payload.GroupId.Should().Be(groupId);
        payload.Front.Label.Should().Be("hello");
        payload.Front.Example.Should().BeEmpty();
        payload.Front.Comment.Should().BeEmpty();
        payload.Back.Label.Should().Be("czesc");
        payload.Back.Example.Should().BeEmpty();
        payload.Back.Comment.Should().BeEmpty();

        var card = await fixture.ExecuteCardsDbContextAsync(db =>
            db.Cards
                .Include(c => c.FrontSide)
                .Include(c => c.BackSide)
                .SingleAsync(c => c.Id == payload.Id));

        card.GroupId.Should().Be(groupId);
        card.FrontSide.Label.Should().Be("hello");
        card.BackSide.Label.Should().Be("czesc");
    }

    [Fact]
    public async Task AddCardToGroup_WithOptionalFields_ShouldPersistThem()
    {
        var (externalUserId, groupId) = await SeedUserAndGroupAsync();

        var request = new AddCardToGroupRequest(
            UserId: externalUserId,
            GroupId: groupId,
            FrontLabel: "run",
            BackLabel: "biegac",
            FrontExample: "I run.",
            FrontComment: "verb",
            BackExample: "Biegam.",
            BackComment: "czasownik");

        var response = await fixture.Client.PostAsJsonAsync("/api/cards/cards", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var payload = await response.Content.ReadFromJsonAsync<CardDto>();
        payload!.Front.Example.Should().Be("I run.");
        payload.Front.Comment.Should().Be("verb");
        payload.Back.Example.Should().Be("Biegam.");
        payload.Back.Comment.Should().Be("czasownik");
    }

    [Fact]
    public async Task AddCardToGroup_WithUnknownUser_ShouldReturnNotFound()
    {
        var request = new AddCardToGroupRequest(
            UserId: Guid.NewGuid(),
            GroupId: 1,
            FrontLabel: "a",
            BackLabel: "b");

        var response = await fixture.Client.PostAsJsonAsync("/api/cards/cards", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var errorPayload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        errorPayload!.Errors.Should().Contain(x => x.Code == "cards.add_card.user.not_found");
    }

    [Fact]
    public async Task AddCardToGroup_WithUnknownGroup_ShouldReturnNotFound()
    {
        var externalUserId = Guid.NewGuid();
        await fixture.ExecuteCardsDbContextAsync(async db =>
        {
            await db.Users.AddAsync(new User { ExternalUserId = externalUserId });
            await db.SaveChangesAsync();
            return 0;
        });

        var request = new AddCardToGroupRequest(
            UserId: externalUserId,
            GroupId: 999_999,
            FrontLabel: "a",
            BackLabel: "b");

        var response = await fixture.Client.PostAsJsonAsync("/api/cards/cards", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var errorPayload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        errorPayload!.Errors.Should().Contain(x => x.Code == "cards.add_card.group.not_found");
    }

    [Fact]
    public async Task AddCardToGroup_WhenGroupBelongsToAnotherUser_ShouldReturnForbidden()
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

        var request = new AddCardToGroupRequest(
            UserId: otherId,
            GroupId: groupId,
            FrontLabel: "x",
            BackLabel: "y");

        var response = await fixture.Client.PostAsJsonAsync("/api/cards/cards", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var errorPayload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        errorPayload!.Errors.Should().Contain(x => x.Code == "cards.add_card.group.forbidden");
    }

    [Fact]
    public async Task AddCardToGroup_WithInvalidPayload_ShouldReturnBadRequest()
    {
        var request = new AddCardToGroupRequest(
            UserId: Guid.Empty,
            GroupId: 0,
            FrontLabel: "",
            BackLabel: " ");

        var response = await fixture.Client.PostAsJsonAsync("/api/cards/cards", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errorPayload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        errorPayload!.Errors.Should().Contain(x => x.Code == "cards.validation.user_id.required");
        errorPayload.Errors.Should().Contain(x => x.Code == "cards.validation.group_id.invalid");
        errorPayload.Errors.Should().Contain(x => x.Code == "cards.validation.front_label.required");
        errorPayload.Errors.Should().Contain(x => x.Code == "cards.validation.back_label.required");
    }

    private async Task<(Guid ExternalUserId, long GroupId)> SeedUserAndGroupAsync()
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
                Name = $"G-{Guid.NewGuid():N}",
                FrontSideType = "EN",
                BackSideType = "PL",
                Type = GroupType.UserOwned,
                UserId = user.Id,
                Cards = []
            };
            await db.Groups.AddAsync(group);
            await db.SaveChangesAsync();
            groupId = group.Id;
            return 0;
        });

        return (externalUserId, groupId);
    }

    private sealed record ErrorResponse(IReadOnlyList<ErrorItem> Errors);
    private sealed record ErrorItem(string Code, string Message, string? Field);
}
