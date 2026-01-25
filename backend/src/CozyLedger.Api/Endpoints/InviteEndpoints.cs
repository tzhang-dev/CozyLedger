using System.Security.Claims;
using System.Security.Cryptography;
using CozyLedger.Api.Extensions;
using CozyLedger.Domain.Entities;
using CozyLedger.Infrastructure.Data;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace CozyLedger.Api.Endpoints;

public static class InviteEndpoints
{
    private const int InviteTokenBytes = 32;
    private static readonly TimeSpan InviteLifetime = TimeSpan.FromDays(7);

    public static IEndpointRouteBuilder MapInviteEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/books/{bookId:guid}/invites", CreateInviteAsync)
            .RequireAuthorization();

        app.MapPost("/invites/{token}/accept", AcceptInviteAsync)
            .RequireAuthorization();

        return app;
    }

    private static async Task<IResult> CreateInviteAsync(
        Guid bookId,
        ClaimsPrincipal user,
        AppDbContext dbContext,
        HttpRequest request)
    {
        var userId = user.GetUserId();
        var isMember = await dbContext.Memberships.AnyAsync(m => m.BookId == bookId && m.UserId == userId);
        if (!isMember)
        {
            return Results.Forbid();
        }

        var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(InviteTokenBytes));
        var expiresAt = DateTime.UtcNow.Add(InviteLifetime);

        var invite = new BookInvite
        {
            Id = Guid.NewGuid(),
            BookId = bookId,
            Token = token,
            ExpiresAtUtc = expiresAt,
            CreatedByUserId = userId
        };

        dbContext.BookInvites.Add(invite);
        await dbContext.SaveChangesAsync();

        var inviteUrl = $"{request.Scheme}://{request.Host}/invites/{token}";
        return Results.Ok(new InviteResponse(token, inviteUrl, expiresAt));
    }

    private static async Task<IResult> AcceptInviteAsync(
        string token,
        ClaimsPrincipal user,
        AppDbContext dbContext)
    {
        var invite = await dbContext.BookInvites
            .FirstOrDefaultAsync(i => i.Token == token && !i.IsUsed);

        if (invite is null || invite.ExpiresAtUtc < DateTime.UtcNow)
        {
            return Results.NotFound();
        }

        var userId = user.GetUserId();
        var alreadyMember = await dbContext.Memberships.AnyAsync(m => m.BookId == invite.BookId && m.UserId == userId);
        if (!alreadyMember)
        {
            dbContext.Memberships.Add(new Membership
            {
                Id = Guid.NewGuid(),
                BookId = invite.BookId,
                UserId = userId
            });
        }

        invite.IsUsed = true;
        invite.UsedByUserId = userId;
        invite.UsedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();

        return Results.Ok(new AcceptInviteResponse(invite.BookId));
    }

    public record InviteResponse(string Token, string InviteUrl, DateTime ExpiresAtUtc);

    public record AcceptInviteResponse(Guid BookId);
}
