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

public class ReportEndpointsTests : IClassFixture<PostgresContainerFixture>
{
    private readonly PostgresContainerFixture _fixture;

    public ReportEndpointsTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

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

    private static async Task<Guid> CreateBookAsync(HttpClient client, string name, string baseCurrency)
    {
        var response = await client.PostAsJsonAsync("/books", new CreateBookRequest(name, baseCurrency));
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<BookResponse>();
        return result!.Id;
    }

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

    private static async Task SeedExchangeRatesAsync(TestWebApplicationFactory factory, params ExchangeRate[] rates)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.ExchangeRates.AddRange(rates);
        await dbContext.SaveChangesAsync();
    }

    private static async Task EnsureDatabaseAsync(TestWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }

    private record RegisterRequest(string Email, string Password, string? DisplayName, string? Locale);

    private record AuthResponse(string Token, DateTime ExpiresAtUtc);

    private record CreateBookRequest(string Name, string? BaseCurrency);

    private record BookResponse(Guid Id, string Name, string BaseCurrency);

    private record AccountRequest(
        string NameEn,
        string NameZhHans,
        AccountType Type,
        string Currency,
        bool IsHidden,
        bool IncludeInNetWorth,
        string? Note);

    private record AccountResponse(
        Guid Id,
        string NameEn,
        string NameZhHans,
        AccountType Type,
        string Currency,
        bool IsHidden,
        bool IncludeInNetWorth,
        string? Note);

    private record CategoryRequest(
        string NameEn,
        string NameZhHans,
        CategoryType Type,
        Guid? ParentId,
        bool IsActive);

    private record CategoryResponse(
        Guid Id,
        string NameEn,
        string NameZhHans,
        CategoryType Type,
        Guid? ParentId,
        bool IsActive);

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

    private record SummaryResponse(
        string BaseCurrency,
        DateTime PeriodStartUtc,
        DateTime PeriodEndExclusiveUtc,
        decimal IncomeTotal,
        decimal ExpenseTotal,
        decimal NetTotal);

    private record CategoryDistributionResponse(
        string BaseCurrency,
        DateTime PeriodStartUtc,
        DateTime PeriodEndExclusiveUtc,
        CategoryType Type,
        List<CategoryDistributionItem> Items);

    private record CategoryDistributionItem(
        Guid CategoryId,
        string CategoryNameEn,
        string CategoryNameZhHans,
        decimal TotalBaseAmount);
}
