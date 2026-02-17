using System.Security.Claims;
using CozyLedger.Api.Extensions;
using CozyLedger.Domain.Entities;
using CozyLedger.Domain.Enums;
using CozyLedger.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CozyLedger.Api.Endpoints;

/// <summary>
/// Defines reporting API endpoints for summaries and category distributions.
/// </summary>
public static class ReportEndpoints
{
    /// <summary>
    /// Maps report endpoints onto the route builder.
    /// </summary>
    /// <param name="app">Route builder to configure.</param>
    /// <returns>The original route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/books/{bookId:guid}/reports").RequireAuthorization();

        group.MapGet("/summary/monthly", GetMonthlySummaryAsync);
        group.MapGet("/summary/yearly", GetYearlySummaryAsync);
        group.MapGet("/categories", GetCategoryDistributionAsync);

        return app;
    }

    /// <summary>
    /// Builds a monthly summary report for the specified period.
    /// </summary>
    /// <param name="bookId">Book identifier.</param>
    /// <param name="year">UTC report year.</param>
    /// <param name="month">UTC report month.</param>
    /// <param name="user">Authenticated user principal.</param>
    /// <param name="dbContext">Database context.</param>
    /// <returns>HTTP result containing monthly summary totals.</returns>
    private static async Task<IResult> GetMonthlySummaryAsync(
        Guid bookId,
        int year,
        int month,
        ClaimsPrincipal user,
        AppDbContext dbContext)
    {
        if (month is < 1 or > 12)
        {
            return Results.BadRequest(new { error = "Month must be between 1 and 12." });
        }

        var periodStartUtc = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEndExclusiveUtc = periodStartUtc.AddMonths(1);

        return await BuildSummaryResponseAsync(bookId, periodStartUtc, periodEndExclusiveUtc, user, dbContext);
    }

    /// <summary>
    /// Builds a yearly summary report for the specified year.
    /// </summary>
    /// <param name="bookId">Book identifier.</param>
    /// <param name="year">UTC report year.</param>
    /// <param name="user">Authenticated user principal.</param>
    /// <param name="dbContext">Database context.</param>
    /// <returns>HTTP result containing yearly summary totals.</returns>
    private static async Task<IResult> GetYearlySummaryAsync(
        Guid bookId,
        int year,
        ClaimsPrincipal user,
        AppDbContext dbContext)
    {
        var periodStartUtc = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEndExclusiveUtc = periodStartUtc.AddYears(1);

        return await BuildSummaryResponseAsync(bookId, periodStartUtc, periodEndExclusiveUtc, user, dbContext);
    }

    /// <summary>
    /// Builds a summary response by aggregating income and expense totals in base currency.
    /// </summary>
    /// <param name="bookId">Book identifier.</param>
    /// <param name="periodStartUtc">Inclusive report period start in UTC.</param>
    /// <param name="periodEndExclusiveUtc">Exclusive report period end in UTC.</param>
    /// <param name="user">Authenticated user principal.</param>
    /// <param name="dbContext">Database context.</param>
    /// <returns>HTTP result containing summary data or validation errors.</returns>
    private static async Task<IResult> BuildSummaryResponseAsync(
        Guid bookId,
        DateTime periodStartUtc,
        DateTime periodEndExclusiveUtc,
        ClaimsPrincipal user,
        AppDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (!await IsMemberAsync(dbContext, bookId, userId))
        {
            return Results.Forbid();
        }

        var book = await dbContext.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == bookId);
        if (book is null)
        {
            return Results.NotFound();
        }

        var transactions = await dbContext.Transactions
            .AsNoTracking()
            .Where(t => t.BookId == bookId
                && t.DateUtc >= periodStartUtc
                && t.DateUtc < periodEndExclusiveUtc
                && (t.Type == TransactionType.Income || t.Type == TransactionType.Expense))
            .ToListAsync();

        var conversion = await ConvertTransactionsToBaseCurrencyAsync(transactions, book.BaseCurrency, periodEndExclusiveUtc, dbContext);
        if (conversion.MissingRateErrors.Count > 0)
        {
            return Results.UnprocessableEntity(new
            {
                error = "Missing exchange rate data for one or more transactions.",
                missingRates = conversion.MissingRateErrors
            });
        }

        var incomeTotal = conversion.Converted
            .Where(t => t.Type == TransactionType.Income)
            .Sum(t => t.BaseAmount);
        var expenseTotal = conversion.Converted
            .Where(t => t.Type == TransactionType.Expense)
            .Sum(t => t.BaseAmount);

        return Results.Ok(new SummaryReportResponse(
            book.BaseCurrency,
            periodStartUtc,
            periodEndExclusiveUtc,
            incomeTotal,
            expenseTotal,
            incomeTotal - expenseTotal));
    }

    /// <summary>
    /// Builds category distribution totals for a year or month and a category type.
    /// </summary>
    /// <param name="bookId">Book identifier.</param>
    /// <param name="year">UTC report year.</param>
    /// <param name="month">Optional UTC report month.</param>
    /// <param name="type">Category type to aggregate.</param>
    /// <param name="user">Authenticated user principal.</param>
    /// <param name="dbContext">Database context.</param>
    /// <returns>HTTP result containing category distribution totals.</returns>
    private static async Task<IResult> GetCategoryDistributionAsync(
        Guid bookId,
        int year,
        int? month,
        CategoryType type,
        ClaimsPrincipal user,
        AppDbContext dbContext)
    {
        if (month.HasValue && month is < 1 or > 12)
        {
            return Results.BadRequest(new { error = "Month must be between 1 and 12 when provided." });
        }

        var periodStartUtc = new DateTime(year, month ?? 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEndExclusiveUtc = month.HasValue
            ? periodStartUtc.AddMonths(1)
            : periodStartUtc.AddYears(1);

        var userId = user.GetUserId();
        if (!await IsMemberAsync(dbContext, bookId, userId))
        {
            return Results.Forbid();
        }

        var book = await dbContext.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == bookId);
        if (book is null)
        {
            return Results.NotFound();
        }

        var transactionType = type == CategoryType.Income
            ? TransactionType.Income
            : TransactionType.Expense;

        var rows = await dbContext.Transactions
            .AsNoTracking()
            .Where(t => t.BookId == bookId
                && t.DateUtc >= periodStartUtc
                && t.DateUtc < periodEndExclusiveUtc
                && t.Type == transactionType
                && t.CategoryId != null)
            .Join(
                dbContext.Categories.AsNoTracking(),
                transaction => transaction.CategoryId,
                category => category.Id,
                (transaction, category) => new CategoryTransactionRow(
                    transaction.Id,
                    transaction.DateUtc,
                    transaction.Type,
                    transaction.Amount,
                    transaction.Currency,
                    category.Id,
                    category.NameEn,
                    category.NameZhHans))
            .ToListAsync();

        var transactionsForConversion = rows
            .Select(r => new Transaction
            {
                Id = r.TransactionId,
                DateUtc = r.DateUtc,
                Type = r.Type,
                Amount = r.Amount,
                Currency = r.Currency
            })
            .ToList();

        var conversion = await ConvertTransactionsToBaseCurrencyAsync(transactionsForConversion, book.BaseCurrency, periodEndExclusiveUtc, dbContext);
        if (conversion.MissingRateErrors.Count > 0)
        {
            return Results.UnprocessableEntity(new
            {
                error = "Missing exchange rate data for one or more transactions.",
                missingRates = conversion.MissingRateErrors
            });
        }

        var convertedById = conversion.Converted.ToDictionary(t => t.TransactionId, t => t.BaseAmount);

        var categories = rows
            .GroupBy(r => new { r.CategoryId, r.NameEn, r.NameZhHans })
            .Select(group => new CategoryDistributionItemResponse(
                group.Key.CategoryId,
                group.Key.NameEn,
                group.Key.NameZhHans,
                group.Sum(row => convertedById[row.TransactionId])))
            .OrderByDescending(r => Math.Abs(r.TotalBaseAmount))
            .ToList();

        return Results.Ok(new CategoryDistributionResponse(
            book.BaseCurrency,
            periodStartUtc,
            periodEndExclusiveUtc,
            type,
            categories));
    }

    /// <summary>
    /// Converts transactions to report base currency using latest applicable exchange rates.
    /// </summary>
    /// <param name="transactions">Transactions to convert.</param>
    /// <param name="baseCurrency">Target base currency code.</param>
    /// <param name="reportEndExclusiveUtc">Exclusive report end timestamp used to bound rate lookup.</param>
    /// <param name="dbContext">Database context.</param>
    /// <returns>Converted values and missing-rate diagnostics.</returns>
    private static async Task<ConversionResult> ConvertTransactionsToBaseCurrencyAsync(
        IReadOnlyCollection<Transaction> transactions,
        string baseCurrency,
        DateTime reportEndExclusiveUtc,
        AppDbContext dbContext)
    {
        var normalizedBaseCurrency = baseCurrency.ToUpperInvariant();
        var usedCurrencies = transactions
            .Select(t => t.Currency.ToUpperInvariant())
            .Where(c => c != normalizedBaseCurrency)
            .Distinct()
            .ToList();

        var rates = usedCurrencies.Count == 0
            ? []
            : await dbContext.ExchangeRates
                .AsNoTracking()
                .Where(r => r.EffectiveDateUtc < reportEndExclusiveUtc
                    && ((usedCurrencies.Contains(r.BaseCurrency) && r.QuoteCurrency == normalizedBaseCurrency)
                        || (r.BaseCurrency == normalizedBaseCurrency && usedCurrencies.Contains(r.QuoteCurrency))))
                .ToListAsync();

        var converted = new List<ConvertedTransaction>(transactions.Count);
        var missingRateErrors = new List<string>();

        foreach (var transaction in transactions)
        {
            var transactionCurrency = transaction.Currency.ToUpperInvariant();
            if (transactionCurrency == normalizedBaseCurrency)
            {
                converted.Add(new ConvertedTransaction(transaction.Id, transaction.Type, transaction.Amount));
                continue;
            }

            var transactionDate = transaction.DateUtc.Date;

            var directRate = rates
                .Where(r => r.BaseCurrency == transactionCurrency
                    && r.QuoteCurrency == normalizedBaseCurrency
                    && r.EffectiveDateUtc.Date <= transactionDate)
                .OrderByDescending(r => r.EffectiveDateUtc)
                .FirstOrDefault();

            if (directRate is not null)
            {
                converted.Add(new ConvertedTransaction(transaction.Id, transaction.Type, transaction.Amount * directRate.Rate));
                continue;
            }

            var inverseRate = rates
                .Where(r => r.BaseCurrency == normalizedBaseCurrency
                    && r.QuoteCurrency == transactionCurrency
                    && r.EffectiveDateUtc.Date <= transactionDate
                    && r.Rate != 0)
                .OrderByDescending(r => r.EffectiveDateUtc)
                .FirstOrDefault();

            if (inverseRate is not null)
            {
                converted.Add(new ConvertedTransaction(transaction.Id, transaction.Type, transaction.Amount / inverseRate.Rate));
                continue;
            }

            missingRateErrors.Add($"{transactionCurrency}->{normalizedBaseCurrency} on or before {transactionDate:yyyy-MM-dd}");
        }

        return new ConversionResult(converted, missingRateErrors);
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
    /// Represents joined transaction-category data used for category aggregation.
    /// </summary>
    /// <param name="TransactionId">Transaction identifier.</param>
    /// <param name="DateUtc">Transaction date in UTC.</param>
    /// <param name="Type">Transaction type.</param>
    /// <param name="Amount">Transaction amount.</param>
    /// <param name="Currency">Transaction currency code.</param>
    /// <param name="CategoryId">Category identifier.</param>
    /// <param name="NameEn">English category name.</param>
    /// <param name="NameZhHans">Simplified Chinese category name.</param>
    private record CategoryTransactionRow(
        Guid TransactionId,
        DateTime DateUtc,
        TransactionType Type,
        decimal Amount,
        string Currency,
        Guid CategoryId,
        string NameEn,
        string NameZhHans);

    /// <summary>
    /// Represents a converted transaction amount in report base currency.
    /// </summary>
    /// <param name="TransactionId">Transaction identifier.</param>
    /// <param name="Type">Transaction type.</param>
    /// <param name="BaseAmount">Amount converted into report base currency.</param>
    private record ConvertedTransaction(Guid TransactionId, TransactionType Type, decimal BaseAmount);

    /// <summary>
    /// Represents conversion output including converted values and missing rate messages.
    /// </summary>
    /// <param name="Converted">Converted transaction amounts.</param>
    /// <param name="MissingRateErrors">Messages describing missing rate data.</param>
    private record ConversionResult(List<ConvertedTransaction> Converted, List<string> MissingRateErrors);

    /// <summary>
    /// Represents summary report totals for a period.
    /// </summary>
    /// <param name="BaseCurrency">Report base currency code.</param>
    /// <param name="PeriodStartUtc">Inclusive period start in UTC.</param>
    /// <param name="PeriodEndExclusiveUtc">Exclusive period end in UTC.</param>
    /// <param name="IncomeTotal">Total income converted into base currency.</param>
    /// <param name="ExpenseTotal">Total expense converted into base currency.</param>
    /// <param name="NetTotal">Net total equal to income minus expense.</param>
    public record SummaryReportResponse(
        string BaseCurrency,
        DateTime PeriodStartUtc,
        DateTime PeriodEndExclusiveUtc,
        decimal IncomeTotal,
        decimal ExpenseTotal,
        decimal NetTotal);

    /// <summary>
    /// Represents category distribution totals for a report period.
    /// </summary>
    /// <param name="BaseCurrency">Report base currency code.</param>
    /// <param name="PeriodStartUtc">Inclusive period start in UTC.</param>
    /// <param name="PeriodEndExclusiveUtc">Exclusive period end in UTC.</param>
    /// <param name="Type">Category type included in the distribution.</param>
    /// <param name="Items">Category total items sorted by absolute amount descending.</param>
    public record CategoryDistributionResponse(
        string BaseCurrency,
        DateTime PeriodStartUtc,
        DateTime PeriodEndExclusiveUtc,
        CategoryType Type,
        List<CategoryDistributionItemResponse> Items);

    /// <summary>
    /// Represents one category total item in a distribution report.
    /// </summary>
    /// <param name="CategoryId">Category identifier.</param>
    /// <param name="CategoryNameEn">English category name.</param>
    /// <param name="CategoryNameZhHans">Simplified Chinese category name.</param>
    /// <param name="TotalBaseAmount">Total amount converted into report base currency.</param>
    public record CategoryDistributionItemResponse(
        Guid CategoryId,
        string CategoryNameEn,
        string CategoryNameZhHans,
        decimal TotalBaseAmount);
}
