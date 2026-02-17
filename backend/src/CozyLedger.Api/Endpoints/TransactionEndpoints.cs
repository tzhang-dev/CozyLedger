using System.Security.Claims;
using CozyLedger.Api.Extensions;
using CozyLedger.Domain.Entities;
using CozyLedger.Domain.Enums;
using CozyLedger.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CozyLedger.Api.Endpoints;

/// <summary>
/// Defines transaction management API endpoints.
/// </summary>
public static class TransactionEndpoints
{
    /// <summary>
    /// Maps transaction endpoints onto the route builder.
    /// </summary>
    /// <param name="app">Route builder to configure.</param>
    /// <returns>The original route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/books/{bookId:guid}/transactions").RequireAuthorization();

        group.MapGet("/", ListTransactionsAsync);
        group.MapPost("/", CreateTransactionAsync);
        group.MapPut("/{transactionId:guid}", UpdateTransactionAsync);

        return app;
    }

    /// <summary>
    /// Lists transactions in a book for an authorized member.
    /// </summary>
    /// <param name="bookId">Book identifier.</param>
    /// <param name="user">Authenticated user principal.</param>
    /// <param name="dbContext">Database context.</param>
    /// <returns>HTTP result containing transaction records.</returns>
    private static async Task<IResult> ListTransactionsAsync(Guid bookId, ClaimsPrincipal user, AppDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (!await IsMemberAsync(dbContext, bookId, userId))
        {
            return Results.Forbid();
        }

        var transactions = await dbContext.Transactions
            .AsNoTracking()
            .Where(t => t.BookId == bookId)
            .OrderByDescending(t => t.DateUtc)
            .ThenByDescending(t => t.CreatedAtUtc)
            .Select(t => new TransactionResponse(
                t.Id,
                t.Type,
                t.DateUtc,
                t.Amount,
                t.Currency,
                t.AccountId,
                t.ToAccountId,
                t.CategoryId,
                t.MemberId,
                t.Note,
                t.IsRefund,
                t.CreatedAtUtc))
            .ToListAsync();

        return Results.Ok(transactions);
    }

    /// <summary>
    /// Creates a transaction in the specified book.
    /// </summary>
    /// <param name="bookId">Book identifier.</param>
    /// <param name="request">Transaction creation payload.</param>
    /// <param name="user">Authenticated user principal.</param>
    /// <param name="dbContext">Database context.</param>
    /// <returns>HTTP result containing the created transaction.</returns>
    private static async Task<IResult> CreateTransactionAsync(
        Guid bookId,
        CreateTransactionRequest request,
        ClaimsPrincipal user,
        AppDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (!await IsMemberAsync(dbContext, bookId, userId))
        {
            return Results.Forbid();
        }

        var validationError = await ValidateTransactionAsync(dbContext, bookId, request);
        if (validationError is not null)
        {
            return Results.BadRequest(new { error = validationError });
        }

        var amount = NormalizeAmount(request.Type, request.Amount, request.IsRefund);
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            BookId = bookId,
            Type = request.Type,
            DateUtc = request.DateUtc,
            Amount = amount,
            Currency = request.Currency.ToUpperInvariant(),
            AccountId = request.AccountId,
            ToAccountId = request.ToAccountId,
            CategoryId = request.CategoryId,
            MemberId = request.MemberId,
            Note = request.Note,
            IsRefund = request.IsRefund,
            CreatedByUserId = userId
        };

        dbContext.Transactions.Add(transaction);
        await dbContext.SaveChangesAsync();

        return Results.Created($"/books/{bookId}/transactions/{transaction.Id}", new TransactionResponse(
            transaction.Id,
            transaction.Type,
            transaction.DateUtc,
            transaction.Amount,
            transaction.Currency,
            transaction.AccountId,
            transaction.ToAccountId,
            transaction.CategoryId,
            transaction.MemberId,
            transaction.Note,
            transaction.IsRefund,
            transaction.CreatedAtUtc));
    }

    /// <summary>
    /// Updates an existing transaction in the specified book.
    /// </summary>
    /// <param name="bookId">Book identifier.</param>
    /// <param name="transactionId">Transaction identifier.</param>
    /// <param name="request">Transaction update payload.</param>
    /// <param name="user">Authenticated user principal.</param>
    /// <param name="dbContext">Database context.</param>
    /// <returns>HTTP result containing the updated transaction.</returns>
    private static async Task<IResult> UpdateTransactionAsync(
        Guid bookId,
        Guid transactionId,
        UpdateTransactionRequest request,
        ClaimsPrincipal user,
        AppDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (!await IsMemberAsync(dbContext, bookId, userId))
        {
            return Results.Forbid();
        }

        var transaction = await dbContext.Transactions.FirstOrDefaultAsync(t => t.Id == transactionId && t.BookId == bookId);
        if (transaction is null)
        {
            return Results.NotFound();
        }

        var validationError = await ValidateTransactionAsync(dbContext, bookId, request.ToCreateRequest(transaction.AccountId));
        if (validationError is not null)
        {
            return Results.BadRequest(new { error = validationError });
        }

        transaction.Type = request.Type;
        transaction.DateUtc = request.DateUtc;
        transaction.Amount = NormalizeAmount(request.Type, request.Amount, request.IsRefund);
        transaction.Currency = request.Currency.ToUpperInvariant();
        transaction.AccountId = request.AccountId;
        transaction.ToAccountId = request.ToAccountId;
        transaction.CategoryId = request.CategoryId;
        transaction.MemberId = request.MemberId;
        transaction.Note = request.Note;
        transaction.IsRefund = request.IsRefund;

        await dbContext.SaveChangesAsync();

        return Results.Ok(new TransactionResponse(
            transaction.Id,
            transaction.Type,
            transaction.DateUtc,
            transaction.Amount,
            transaction.Currency,
            transaction.AccountId,
            transaction.ToAccountId,
            transaction.CategoryId,
            transaction.MemberId,
            transaction.Note,
            transaction.IsRefund,
            transaction.CreatedAtUtc));
    }

    /// <summary>
    /// Normalizes amount sign based on transaction type and refund state.
    /// </summary>
    /// <param name="type">Transaction type.</param>
    /// <param name="amount">Input amount.</param>
    /// <param name="isRefund">Whether the transaction is a refund.</param>
    /// <returns>Normalized amount value.</returns>
    private static decimal NormalizeAmount(TransactionType type, decimal amount, bool isRefund)
    {
        var normalized = Math.Abs(amount);
        if (type == TransactionType.Expense && isRefund)
        {
            return -normalized;
        }

        return normalized;
    }

    /// <summary>
    /// Validates transaction constraints before persistence.
    /// </summary>
    /// <param name="dbContext">Database context.</param>
    /// <param name="bookId">Book identifier.</param>
    /// <param name="request">Transaction payload to validate.</param>
    /// <returns>Error message when invalid; otherwise <see langword="null"/>.</returns>
    private static async Task<string?> ValidateTransactionAsync(AppDbContext dbContext, Guid bookId, CreateTransactionRequest request)
    {
        if (request.Amount == 0)
        {
            return "Amount must be non-zero.";
        }

        if (string.IsNullOrWhiteSpace(request.Currency) || request.Currency.Length != 3)
        {
            return "Currency code must be 3 letters.";
        }

        if (!await dbContext.Accounts.AnyAsync(a => a.Id == request.AccountId && a.BookId == bookId))
        {
            return "Account not found.";
        }

        if (request.Type == TransactionType.Transfer)
        {
            if (request.ToAccountId is null)
            {
                return "Transfer requires a destination account.";
            }

            if (request.ToAccountId == request.AccountId)
            {
                return "Transfer accounts must be different.";
            }

            if (!await dbContext.Accounts.AnyAsync(a => a.Id == request.ToAccountId && a.BookId == bookId))
            {
                return "Destination account not found.";
            }

            if (request.CategoryId is not null)
            {
                return "Transfer cannot have a category.";
            }

            if (request.IsRefund)
            {
                return "Transfer cannot be a refund.";
            }
        }

        if (request.Type == TransactionType.Income || request.Type == TransactionType.Expense)
        {
            if (request.CategoryId is null)
            {
                return "Income and expense require a category.";
            }

            if (!await dbContext.Categories.AnyAsync(c => c.Id == request.CategoryId && c.BookId == bookId))
            {
                return "Category not found.";
            }

            if (request.ToAccountId is not null)
            {
                return "Income and expense cannot have a destination account.";
            }

            if (request.Type == TransactionType.Income && request.IsRefund)
            {
                return "Income cannot be marked as refund.";
            }
        }

        if (request.Type == TransactionType.BalanceAdjustment)
        {
            if (request.CategoryId is not null || request.ToAccountId is not null)
            {
                return "Balance adjustment cannot have category or destination account.";
            }

            if (request.IsRefund)
            {
                return "Balance adjustment cannot be a refund.";
            }
        }

        return null;
    }

    /// <summary>
    /// Determines whether a user is a member of the specified book.
    /// </summary>
    /// <param name="dbContext">Database context.</param>
    /// <param name="bookId">Book identifier.</param>
    /// <param name="userId">User identifier.</param>
    /// <returns><see langword="true"/> when membership exists; otherwise, <see langword="false"/>.</returns>
    private static Task<bool> IsMemberAsync(AppDbContext dbContext, Guid bookId, Guid userId)
        => dbContext.Memberships.AnyAsync(m => m.BookId == bookId && m.UserId == userId);

    /// <summary>
    /// Represents payload for transaction creation.
    /// </summary>
    /// <param name="Type">Transaction type.</param>
    /// <param name="DateUtc">Transaction date in UTC.</param>
    /// <param name="Amount">Transaction amount.</param>
    /// <param name="Currency">ISO 4217 currency code.</param>
    /// <param name="AccountId">Source account identifier.</param>
    /// <param name="ToAccountId">Optional destination account identifier.</param>
    /// <param name="CategoryId">Optional category identifier.</param>
    /// <param name="MemberId">Optional related member identifier.</param>
    /// <param name="Note">Optional note.</param>
    /// <param name="IsRefund">Whether the transaction is a refund.</param>
    public record CreateTransactionRequest(
        TransactionType Type,
        DateTime DateUtc,
        decimal Amount,
        string Currency,
        Guid AccountId,
        Guid? ToAccountId,
        Guid? CategoryId,
        Guid? MemberId,
        string? Note,
        bool IsRefund);

    /// <summary>
    /// Represents payload for transaction updates.
    /// </summary>
    /// <param name="Type">Transaction type.</param>
    /// <param name="DateUtc">Transaction date in UTC.</param>
    /// <param name="Amount">Transaction amount.</param>
    /// <param name="Currency">ISO 4217 currency code.</param>
    /// <param name="AccountId">Source account identifier.</param>
    /// <param name="ToAccountId">Optional destination account identifier.</param>
    /// <param name="CategoryId">Optional category identifier.</param>
    /// <param name="MemberId">Optional related member identifier.</param>
    /// <param name="Note">Optional note.</param>
    /// <param name="IsRefund">Whether the transaction is a refund.</param>
    public record UpdateTransactionRequest(
        TransactionType Type,
        DateTime DateUtc,
        decimal Amount,
        string Currency,
        Guid AccountId,
        Guid? ToAccountId,
        Guid? CategoryId,
        Guid? MemberId,
        string? Note,
        bool IsRefund)
    {
        /// <summary>
        /// Converts this update payload into a create payload for shared validation.
        /// </summary>
        /// <param name="fallbackAccountId">Fallback account id used when request account id is empty.</param>
        /// <returns>A create request with equivalent values.</returns>
        public CreateTransactionRequest ToCreateRequest(Guid fallbackAccountId)
        {
            return new CreateTransactionRequest(
                Type,
                DateUtc,
                Amount,
                Currency,
                AccountId == Guid.Empty ? fallbackAccountId : AccountId,
                ToAccountId,
                CategoryId,
                MemberId,
                Note,
                IsRefund);
        }
    }

    /// <summary>
    /// Represents transaction data returned by the API.
    /// </summary>
    /// <param name="Id">Transaction identifier.</param>
    /// <param name="Type">Transaction type.</param>
    /// <param name="DateUtc">Transaction date in UTC.</param>
    /// <param name="Amount">Transaction amount.</param>
    /// <param name="Currency">ISO 4217 currency code.</param>
    /// <param name="AccountId">Source account identifier.</param>
    /// <param name="ToAccountId">Optional destination account identifier.</param>
    /// <param name="CategoryId">Optional category identifier.</param>
    /// <param name="MemberId">Optional related member identifier.</param>
    /// <param name="Note">Optional note.</param>
    /// <param name="IsRefund">Whether the transaction is a refund.</param>
    /// <param name="CreatedAtUtc">Creation timestamp in UTC.</param>
    public record TransactionResponse(
        Guid Id,
        TransactionType Type,
        DateTime DateUtc,
        decimal Amount,
        string Currency,
        Guid AccountId,
        Guid? ToAccountId,
        Guid? CategoryId,
        Guid? MemberId,
        string? Note,
        bool IsRefund,
        DateTime CreatedAtUtc);
}
