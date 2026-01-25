using System.Security.Claims;
using CozyLedger.Api.Extensions;
using CozyLedger.Domain.Entities;
using CozyLedger.Domain.Enums;
using CozyLedger.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CozyLedger.Api.Endpoints;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/books/{bookId:guid}/accounts").RequireAuthorization();

        group.MapGet("/", ListAccountsAsync);
        group.MapPost("/", CreateAccountAsync);
        group.MapPut("/{accountId:guid}", UpdateAccountAsync);

        return app;
    }

    private static async Task<IResult> ListAccountsAsync(Guid bookId, ClaimsPrincipal user, AppDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (!await IsMemberAsync(dbContext, bookId, userId))
        {
            return Results.Forbid();
        }

        var accounts = await dbContext.Accounts
            .AsNoTracking()
            .Where(a => a.BookId == bookId)
            .OrderBy(a => a.NameEn)
            .Select(a => new AccountResponse(
                a.Id,
                a.NameEn,
                a.NameZhHans,
                a.Type,
                a.Currency,
                a.IsHidden,
                a.IncludeInNetWorth,
                a.Note))
            .ToListAsync();

        return Results.Ok(accounts);
    }

    private static async Task<IResult> CreateAccountAsync(
        Guid bookId,
        CreateAccountRequest request,
        ClaimsPrincipal user,
        AppDbContext dbContext)
    {
        if (string.IsNullOrWhiteSpace(request.NameEn) || string.IsNullOrWhiteSpace(request.NameZhHans))
        {
            return Results.BadRequest(new { error = "Account names are required." });
        }

        if (string.IsNullOrWhiteSpace(request.Currency) || request.Currency.Length != 3)
        {
            return Results.BadRequest(new { error = "Currency code must be 3 letters." });
        }

        var userId = user.GetUserId();
        if (!await IsMemberAsync(dbContext, bookId, userId))
        {
            return Results.Forbid();
        }

        var account = new Account
        {
            Id = Guid.NewGuid(),
            BookId = bookId,
            NameEn = request.NameEn,
            NameZhHans = request.NameZhHans,
            Type = request.Type,
            Currency = request.Currency.ToUpperInvariant(),
            IsHidden = request.IsHidden,
            IncludeInNetWorth = request.IncludeInNetWorth,
            Note = request.Note
        };

        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync();

        return Results.Created($"/books/{bookId}/accounts/{account.Id}", new AccountResponse(
            account.Id,
            account.NameEn,
            account.NameZhHans,
            account.Type,
            account.Currency,
            account.IsHidden,
            account.IncludeInNetWorth,
            account.Note));
    }

    private static async Task<IResult> UpdateAccountAsync(
        Guid bookId,
        Guid accountId,
        UpdateAccountRequest request,
        ClaimsPrincipal user,
        AppDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (!await IsMemberAsync(dbContext, bookId, userId))
        {
            return Results.Forbid();
        }

        var account = await dbContext.Accounts.FirstOrDefaultAsync(a => a.Id == accountId && a.BookId == bookId);
        if (account is null)
        {
            return Results.NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.NameEn) || string.IsNullOrWhiteSpace(request.NameZhHans))
        {
            return Results.BadRequest(new { error = "Account names are required." });
        }

        if (string.IsNullOrWhiteSpace(request.Currency) || request.Currency.Length != 3)
        {
            return Results.BadRequest(new { error = "Currency code must be 3 letters." });
        }

        account.NameEn = request.NameEn;
        account.NameZhHans = request.NameZhHans;
        account.Type = request.Type;
        account.Currency = request.Currency.ToUpperInvariant();
        account.IsHidden = request.IsHidden;
        account.IncludeInNetWorth = request.IncludeInNetWorth;
        account.Note = request.Note;

        await dbContext.SaveChangesAsync();

        return Results.Ok(new AccountResponse(
            account.Id,
            account.NameEn,
            account.NameZhHans,
            account.Type,
            account.Currency,
            account.IsHidden,
            account.IncludeInNetWorth,
            account.Note));
    }

    private static Task<bool> IsMemberAsync(AppDbContext dbContext, Guid bookId, Guid userId)
        => dbContext.Memberships.AnyAsync(m => m.BookId == bookId && m.UserId == userId);

    public record CreateAccountRequest(
        string NameEn,
        string NameZhHans,
        AccountType Type,
        string Currency,
        bool IsHidden,
        bool IncludeInNetWorth,
        string? Note);

    public record UpdateAccountRequest(
        string NameEn,
        string NameZhHans,
        AccountType Type,
        string Currency,
        bool IsHidden,
        bool IncludeInNetWorth,
        string? Note);

    public record AccountResponse(
        Guid Id,
        string NameEn,
        string NameZhHans,
        AccountType Type,
        string Currency,
        bool IsHidden,
        bool IncludeInNetWorth,
        string? Note);
}