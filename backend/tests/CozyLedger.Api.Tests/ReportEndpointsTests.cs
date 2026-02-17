using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CozyLedger.Domain.Entities;
using CozyLedger.Domain.Enums;
using CozyLedger.Infrastructure.Data;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CozyLedger.Api.Tests;

/// <summary>
/// Verifies report endpoints for summary conversion and category distribution.
/// </summary>
public class ReportEndpointsTests : IClassFixture<PostgresContainerFixture>
{
    private readonly PostgresContainerFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReportEndpointsTests"/> class.
    /// </summary>
    /// <param name="fixture">Shared PostgreSQL container fixture.</param>
    public ReportEndpointsTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Verifies monthly and yearly summaries use the latest rate effective on or before transaction date.
    /// </summary>
    /// <returns>A task that completes when assertions are validated.</returns>
    [Fact]
    public async Task Monthly_and_yearly_summary_convert_to_base_currency_using_latest_rate_on_or_before_date()
    {
        await using var factory = new TestWebApplicationFactory(_fixture.ConnectionString);
        await EnsureDatabaseAsync(factory);

        using var client = factory.CreateClient();
        var token = await RegisterAndGetToken(client, "report-summary@cozy.local");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var bookId = await CreateBookAsync(client, "Report Book", "USD");
        var accountId = await CreateAccountAsync(client, bookId, "Wallet", "EUR");
        var incomeCategoryId = await CreateCategoryAsync(client, bookId, "Salary", CategoryType.Income);
        var expenseCategoryId = await CreateCategoryAsync(client, bookId, "Food", CategoryType.Expense);

        await SeedExchangeRatesAsync(factory, new ExchangeRate
        {
            Id = Guid.NewGuid(),
            BaseCurrency = "EUR",
            QuoteCurrency = "USD",
            Rate = 1.2m,
            EffectiveDateUtc = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc),
            Source = "test"
        });

        await CreateTransactionAsync(
            client,
            bookId,
            TransactionType.Income,
            new DateTime(2026, 1, 12, 0, 0, 0, DateTimeKind.Utc),
            100m,
            "EUR",
            accountId,
            incomeCategoryId,
            false);

        await CreateTransactionAsync(
            client,
            bookId,
            TransactionType.Expense,
            new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            30m,
            "USD",
            accountId,
            expenseCategoryId,
            false);

        var monthlyResponse = await client.GetAsync($"/books/{bookId}/reports/summary/monthly?year=2026&month=1");
        monthlyResponse.IsSuccessStatusCode.Should().BeTrue();
        var monthly = await monthlyResponse.Content.ReadFromJsonAsync<SummaryResponse>();

        monthly.Should().NotBeNull();
        monthly!.BaseCurrency.Should().Be("USD");
        monthly.IncomeTotal.Should().Be(120m);
        monthly.ExpenseTotal.Should().Be(30m);
        monthly.NetTotal.Should().Be(90m);

        var yearlyResponse = await client.GetAsync($"/books/{bookId}/reports/summary/yearly?year=2026");
        yearlyResponse.IsSuccessStatusCode.Should().BeTrue();
        var yearly = await yearlyResponse.Content.ReadFromJsonAsync<SummaryResponse>();

        yearly.Should().NotBeNull();
        yearly!.IncomeTotal.Should().Be(120m);
        yearly.ExpenseTotal.Should().Be(30m);
        yearly.NetTotal.Should().Be(90m);
    }

    /// <summary>
    /// Verifies category distribution can convert using inverse rates when direct rates are missing.
    /// </summary>
    /// <returns>A task that completes when assertions are validated.</returns>
    [Fact]
    public async Task Category_distribution_uses_inverse_rate_fallback_when_direct_rate_is_missing()
    {
        await using var factory = new TestWebApplicationFactory(_fixture.ConnectionString);
        await EnsureDatabaseAsync(factory);

        using var client = factory.CreateClient();
        var token = await RegisterAndGetToken(client, "report-categories@cozy.local");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var bookId = await CreateBookAsync(client, "Category Book", "USD");
        var accountId = await CreateAccountAsync(client, bookId, "Cash", "USD");
        var foodCategoryId = await CreateCategoryAsync(client, bookId, "Food", CategoryType.Expense);
        var travelCategoryId = await CreateCategoryAsync(client, bookId, "Travel", CategoryType.Expense);

        await SeedExchangeRatesAsync(factory, new ExchangeRate
        {
            Id = Guid.NewGuid(),
            BaseCurrency = "USD",
            QuoteCurrency = "JPY",
            Rate = 100m,
            EffectiveDateUtc = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc),
            Source = "test"
        });

        await CreateTransactionAsync(
            client,
            bookId,
            TransactionType.Expense,
            new DateTime(2026, 1, 7, 0, 0, 0, DateTimeKind.Utc),
            1000m,
            "JPY",
            accountId,
            foodCategoryId,
            false);

        await CreateTransactionAsync(
            client,
            bookId,
            TransactionType.Expense,
            new DateTime(2026, 1, 8, 0, 0, 0, DateTimeKind.Utc),
            20m,
            "USD",
            accountId,
            travelCategoryId,
            false);

        var response = await client.GetAsync($"/books/{bookId}/reports/categories?year=2026&month=1&type=Expense");
        response.IsSuccessStatusCode.Should().BeTrue();

        var distribution = await response.Content.ReadFromJsonAsync<CategoryDistributionResponse>();
        distribution.Should().NotBeNull();
        distribution!.BaseCurrency.Should().Be("USD");
        distribution.Items.Should().HaveCount(2);

        distribution.Items.Single(i => i.CategoryNameEn == "Travel").TotalBaseAmount.Should().Be(20m);
        distribution.Items.Single(i => i.CategoryNameEn == "Food").TotalBaseAmount.Should().Be(10m);
    }

    /// <summary>
    /// Verifies summary endpoint returns unprocessable entity when exchange rates are missing.
    /// </summary>
    /// <returns>A task that completes when assertions are validated.</returns>
    [Fact]
    public async Task Summary_returns_unprocessable_entity_when_exchange_rate_is_missing()
    {
        await using var factory = new TestWebApplicationFactory(_fixture.ConnectionString);
        await EnsureDatabaseAsync(factory);

        using var client = factory.CreateClient();
        var token = await RegisterAndGetToken(client, "report-missing-rate@cozy.local");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var bookId = await CreateBookAsync(client, "Missing Rate Book", "USD");
        var accountId = await CreateAccountAsync(client, bookId, "Wallet", "GBP");
        var incomeCategoryId = await CreateCategoryAsync(client, bookId, "Salary", CategoryType.Income);

        await CreateTransactionAsync(
            client,
            bookId,
            TransactionType.Income,
            new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc),
            200m,
            "GBP",
            accountId,
            incomeCategoryId,
            false);

        var response = await client.GetAsync($"/books/{bookId}/reports/summary/monthly?year=2026&month=1");
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        document.RootElement.GetProperty("error").GetString().Should().NotBeNullOrWhiteSpace();
        document.RootElement.GetProperty("missingRates").GetArrayLength().Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Registers a test user and returns the issued JWT token.
    /// </summary>
    /// <param name="client">HTTP client targeting the test server.</param>
    /// <param name="email">Email address to register.</param>
    /// <returns>JWT token string.</returns>
    private static async Task<string> RegisterAndGetToken(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/auth/register", new RegisterRequest(
            email,
            "Password123",
            email,
            "en"));
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return result!.Token;
    }

    /// <summary>
    /// Creates a book and returns its identifier.
    /// </summary>
    /// <param name="client">HTTP client targeting the test server.</param>
    /// <param name="name">Book name.</param>
    /// <param name="baseCurrency">Book base currency code.</param>
    /// <returns>Created book identifier.</returns>
    private static async Task<Guid> CreateBookAsync(HttpClient client, string name, string baseCurrency)
    {
        var response = await client.PostAsJsonAsync("/books", new CreateBookRequest(name, baseCurrency));
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<BookResponse>();
        return result!.Id;
    }

    /// <summary>
    /// Creates an account in the specified book and returns its identifier.
    /// </summary>
    /// <param name="client">HTTP client targeting the test server.</param>
    /// <param name="bookId">Book identifier.</param>
    /// <param name="name">Account name.</param>
    /// <param name="currency">Account currency code.</param>
    /// <returns>Created account identifier.</returns>
    private static async Task<Guid> CreateAccountAsync(HttpClient client, Guid bookId, string name, string currency)
    {
        var response = await client.PostAsJsonAsync($"/books/{bookId}/accounts", new AccountRequest(
            name,
            name,
            AccountType.Cash,
            currency,
            false,
            true,
            null));
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AccountResponse>();
        return result!.Id;
    }

    /// <summary>
    /// Creates a category in the specified book and returns its identifier.
    /// </summary>
    /// <param name="client">HTTP client targeting the test server.</param>
    /// <param name="bookId">Book identifier.</param>
    /// <param name="name">Category name.</param>
    /// <param name="type">Category type.</param>
    /// <returns>Created category identifier.</returns>
    private static async Task<Guid> CreateCategoryAsync(HttpClient client, Guid bookId, string name, CategoryType type)
    {
        var response = await client.PostAsJsonAsync($"/books/{bookId}/categories", new CategoryRequest(
            name,
            name,
            type,
            null,
            true));
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CategoryResponse>();
        return result!.Id;
    }

    /// <summary>
    /// Creates a transaction used in reporting tests.
    /// </summary>
    /// <param name="client">HTTP client targeting the test server.</param>
    /// <param name="bookId">Book identifier.</param>
    /// <param name="type">Transaction type.</param>
    /// <param name="dateUtc">Transaction date in UTC.</param>
    /// <param name="amount">Amount.</param>
    /// <param name="currency">Currency code.</param>
    /// <param name="accountId">Account identifier.</param>
    /// <param name="categoryId">Category identifier.</param>
    /// <param name="isRefund">Refund flag.</param>
    /// <returns>A task that completes when transaction creation succeeds.</returns>
    private static async Task CreateTransactionAsync(
        HttpClient client,
        Guid bookId,
        TransactionType type,
        DateTime dateUtc,
        decimal amount,
        string currency,
        Guid accountId,
        Guid categoryId,
        bool isRefund)
    {
        var response = await client.PostAsJsonAsync($"/books/{bookId}/transactions", new TransactionRequest(
            type,
            dateUtc,
            amount,
            currency,
            accountId,
            null,
            categoryId,
            null,
            null,
            isRefund));
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Inserts exchange rate rows for reporting conversion tests.
    /// </summary>
    /// <param name="factory">Test application factory.</param>
    /// <param name="rates">Exchange rates to persist.</param>
    /// <returns>A task that completes when rates are saved.</returns>
    private static async Task SeedExchangeRatesAsync(TestWebApplicationFactory factory, params ExchangeRate[] rates)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.ExchangeRates.AddRange(rates);
        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Ensures the test database schema exists.
    /// </summary>
    /// <param name="factory">Test application factory.</param>
    /// <returns>A task that completes when schema setup is done.</returns>
    private static async Task EnsureDatabaseAsync(TestWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }

    /// <summary>
    /// Registration request payload used in tests.
    /// </summary>
    /// <param name="Email">User email address.</param>
    /// <param name="Password">Plain-text password.</param>
    /// <param name="DisplayName">Optional display name.</param>
    /// <param name="Locale">Optional locale code.</param>
    private record RegisterRequest(string Email, string Password, string? DisplayName, string? Locale);

    /// <summary>
    /// Authentication response payload used in tests.
    /// </summary>
    /// <param name="Token">JWT bearer token.</param>
    /// <param name="ExpiresAtUtc">UTC expiration timestamp.</param>
    private record AuthResponse(string Token, DateTime ExpiresAtUtc);

    /// <summary>
    /// Book creation request payload used in tests.
    /// </summary>
    /// <param name="Name">Book name.</param>
    /// <param name="BaseCurrency">Base currency code.</param>
    private record CreateBookRequest(string Name, string? BaseCurrency);

    /// <summary>
    /// Book response payload used in tests.
    /// </summary>
    /// <param name="Id">Book identifier.</param>
    /// <param name="Name">Book name.</param>
    /// <param name="BaseCurrency">Book base currency code.</param>
    private record BookResponse(Guid Id, string Name, string BaseCurrency);

    /// <summary>
    /// Account request payload used in tests.
    /// </summary>
    /// <param name="NameEn">English account name.</param>
    /// <param name="NameZhHans">Simplified Chinese account name.</param>
    /// <param name="Type">Account type.</param>
    /// <param name="Currency">Currency code.</param>
    /// <param name="IsHidden">Hidden flag.</param>
    /// <param name="IncludeInNetWorth">Net worth inclusion flag.</param>
    /// <param name="Note">Optional note.</param>
    private record AccountRequest(
        string NameEn,
        string NameZhHans,
        AccountType Type,
        string Currency,
        bool IsHidden,
        bool IncludeInNetWorth,
        string? Note);

    /// <summary>
    /// Account response payload used in tests.
    /// </summary>
    /// <param name="Id">Account identifier.</param>
    /// <param name="NameEn">English account name.</param>
    /// <param name="NameZhHans">Simplified Chinese account name.</param>
    /// <param name="Type">Account type.</param>
    /// <param name="Currency">Currency code.</param>
    /// <param name="IsHidden">Hidden flag.</param>
    /// <param name="IncludeInNetWorth">Net worth inclusion flag.</param>
    /// <param name="Note">Optional note.</param>
    private record AccountResponse(
        Guid Id,
        string NameEn,
        string NameZhHans,
        AccountType Type,
        string Currency,
        bool IsHidden,
        bool IncludeInNetWorth,
        string? Note);

    /// <summary>
    /// Category request payload used in tests.
    /// </summary>
    /// <param name="NameEn">English category name.</param>
    /// <param name="NameZhHans">Simplified Chinese category name.</param>
    /// <param name="Type">Category type.</param>
    /// <param name="ParentId">Optional parent category identifier.</param>
    /// <param name="IsActive">Active flag.</param>
    private record CategoryRequest(
        string NameEn,
        string NameZhHans,
        CategoryType Type,
        Guid? ParentId,
        bool IsActive);

    /// <summary>
    /// Category response payload used in tests.
    /// </summary>
    /// <param name="Id">Category identifier.</param>
    /// <param name="NameEn">English category name.</param>
    /// <param name="NameZhHans">Simplified Chinese category name.</param>
    /// <param name="Type">Category type.</param>
    /// <param name="ParentId">Optional parent category identifier.</param>
    /// <param name="IsActive">Active flag.</param>
    private record CategoryResponse(
        Guid Id,
        string NameEn,
        string NameZhHans,
        CategoryType Type,
        Guid? ParentId,
        bool IsActive);

    /// <summary>
    /// Transaction request payload used in tests.
    /// </summary>
    /// <param name="Type">Transaction type.</param>
    /// <param name="DateUtc">Transaction date in UTC.</param>
    /// <param name="Amount">Amount.</param>
    /// <param name="Currency">Currency code.</param>
    /// <param name="AccountId">Account identifier.</param>
    /// <param name="ToAccountId">Optional destination account identifier.</param>
    /// <param name="CategoryId">Optional category identifier.</param>
    /// <param name="MemberId">Optional member identifier.</param>
    /// <param name="Note">Optional note.</param>
    /// <param name="IsRefund">Refund flag.</param>
    private record TransactionRequest(
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
    /// Summary response payload used in tests.
    /// </summary>
    /// <param name="BaseCurrency">Base currency code.</param>
    /// <param name="PeriodStartUtc">Period start in UTC.</param>
    /// <param name="PeriodEndExclusiveUtc">Period end in UTC.</param>
    /// <param name="IncomeTotal">Income total.</param>
    /// <param name="ExpenseTotal">Expense total.</param>
    /// <param name="NetTotal">Net total.</param>
    private record SummaryResponse(
        string BaseCurrency,
        DateTime PeriodStartUtc,
        DateTime PeriodEndExclusiveUtc,
        decimal IncomeTotal,
        decimal ExpenseTotal,
        decimal NetTotal);

    /// <summary>
    /// Category distribution response payload used in tests.
    /// </summary>
    /// <param name="BaseCurrency">Base currency code.</param>
    /// <param name="PeriodStartUtc">Period start in UTC.</param>
    /// <param name="PeriodEndExclusiveUtc">Period end in UTC.</param>
    /// <param name="Type">Category type.</param>
    /// <param name="Items">Distribution items.</param>
    private record CategoryDistributionResponse(
        string BaseCurrency,
        DateTime PeriodStartUtc,
        DateTime PeriodEndExclusiveUtc,
        CategoryType Type,
        List<CategoryDistributionItem> Items);

    /// <summary>
    /// Category distribution item payload used in tests.
    /// </summary>
    /// <param name="CategoryId">Category identifier.</param>
    /// <param name="CategoryNameEn">English category name.</param>
    /// <param name="CategoryNameZhHans">Simplified Chinese category name.</param>
    /// <param name="TotalBaseAmount">Total amount in base currency.</param>
    private record CategoryDistributionItem(
        Guid CategoryId,
        string CategoryNameEn,
        string CategoryNameZhHans,
        decimal TotalBaseAmount);
}
