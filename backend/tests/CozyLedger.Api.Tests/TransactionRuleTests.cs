using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CozyLedger.Domain.Enums;
using CozyLedger.Infrastructure.Data;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CozyLedger.Api.Tests;

/// <summary>
/// Verifies transaction validation rules and ordering behavior.
/// </summary>
public class TransactionRuleTests : IClassFixture<PostgresContainerFixture>
{
    private readonly PostgresContainerFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionRuleTests"/> class.
    /// </summary>
    /// <param name="fixture">Shared PostgreSQL container fixture.</param>
    public TransactionRuleTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Verifies that expense refunds are persisted as negative amounts.
    /// </summary>
    /// <returns>A task that completes when assertions are validated.</returns>
    [Fact]
    public async Task Refund_is_stored_as_negative_expense()
    {
        await using var factory = new TestWebApplicationFactory(_fixture.ConnectionString);
        await EnsureDatabaseAsync(factory);

        using var client = factory.CreateClient();
        var token = await RegisterAndGetToken(client, "refund@cozy.local");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var bookId = await CreateBookAsync(client, "Refund Book");
        var accountId = await CreateAccountAsync(client, bookId, "Cash");
        var categoryId = await CreateCategoryAsync(client, bookId, "Groceries", CategoryType.Expense);

        var createResponse = await client.PostAsJsonAsync($"/books/{bookId}/transactions", new TransactionRequest(
            TransactionType.Expense,
            DateTime.UtcNow.Date,
            120m,
            "USD",
            accountId,
            null,
            categoryId,
            null,
            "Refunded item",
            true));

        createResponse.IsSuccessStatusCode.Should().BeTrue();
        var created = await createResponse.Content.ReadFromJsonAsync<TransactionResponse>();
        created!.IsRefund.Should().BeTrue();
        created.Amount.Should().Be(-120m);

        var listResponse = await client.GetAsync($"/books/{bookId}/transactions");
        listResponse.IsSuccessStatusCode.Should().BeTrue();
        var list = await listResponse.Content.ReadFromJsonAsync<List<TransactionResponse>>();
        list.Should().NotBeNull();
        list!.First().Amount.Should().Be(-120m);
    }

    /// <summary>
    /// Verifies that balance adjustments reject category assignments.
    /// </summary>
    /// <returns>A task that completes when assertions are validated.</returns>
    [Fact]
    public async Task Balance_adjustment_requires_no_category()
    {
        await using var factory = new TestWebApplicationFactory(_fixture.ConnectionString);
        await EnsureDatabaseAsync(factory);

        using var client = factory.CreateClient();
        var token = await RegisterAndGetToken(client, "balance@cozy.local");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var bookId = await CreateBookAsync(client, "Balance Book");
        var accountId = await CreateAccountAsync(client, bookId, "Checking");
        var categoryId = await CreateCategoryAsync(client, bookId, "Adjustment", CategoryType.Expense);

        var invalidResponse = await client.PostAsJsonAsync($"/books/{bookId}/transactions", new TransactionRequest(
            TransactionType.BalanceAdjustment,
            DateTime.UtcNow.Date,
            50m,
            "USD",
            accountId,
            null,
            categoryId,
            null,
            "Bad adjustment",
            false));

        invalidResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var validResponse = await client.PostAsJsonAsync($"/books/{bookId}/transactions", new TransactionRequest(
            TransactionType.BalanceAdjustment,
            DateTime.UtcNow.Date,
            50m,
            "USD",
            accountId,
            null,
            null,
            null,
            "Good adjustment",
            false));

        validResponse.IsSuccessStatusCode.Should().BeTrue();
    }

    /// <summary>
    /// Verifies transaction listing is sorted by date descending.
    /// </summary>
    /// <returns>A task that completes when assertions are validated.</returns>
    [Fact]
    public async Task Transaction_list_orders_by_date_desc()
    {
        await using var factory = new TestWebApplicationFactory(_fixture.ConnectionString);
        await EnsureDatabaseAsync(factory);

        using var client = factory.CreateClient();
        var token = await RegisterAndGetToken(client, "order@cozy.local");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var bookId = await CreateBookAsync(client, "Order Book");
        var accountId = await CreateAccountAsync(client, bookId, "Wallet");
        var categoryId = await CreateCategoryAsync(client, bookId, "Food", CategoryType.Expense);

        var olderDate = DateTime.UtcNow.Date.AddDays(-2);
        var newerDate = DateTime.UtcNow.Date.AddDays(-1);

        await client.PostAsJsonAsync($"/books/{bookId}/transactions", new TransactionRequest(
            TransactionType.Expense,
            olderDate,
            10m,
            "USD",
            accountId,
            null,
            categoryId,
            null,
            "Older",
            false));

        await client.PostAsJsonAsync($"/books/{bookId}/transactions", new TransactionRequest(
            TransactionType.Expense,
            newerDate,
            12m,
            "USD",
            accountId,
            null,
            categoryId,
            null,
            "Newer",
            false));

        var listResponse = await client.GetAsync($"/books/{bookId}/transactions");
        listResponse.IsSuccessStatusCode.Should().BeTrue();
        var list = await listResponse.Content.ReadFromJsonAsync<List<TransactionResponse>>();
        list.Should().NotBeNull();
        list!.Count.Should().BeGreaterThanOrEqualTo(2);
        list[0].DateUtc.Should().Be(newerDate);
        list[1].DateUtc.Should().Be(olderDate);
    }

    /// <summary>
    /// Verifies transfer transactions require a destination account.
    /// </summary>
    /// <returns>A task that completes when assertions are validated.</returns>
    [Fact]
    public async Task Transfer_requires_destination_account()
    {
        await using var factory = new TestWebApplicationFactory(_fixture.ConnectionString);
        await EnsureDatabaseAsync(factory);

        using var client = factory.CreateClient();
        var token = await RegisterAndGetToken(client, "transfer@cozy.local");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var bookId = await CreateBookAsync(client, "Transfer Book");
        var accountId = await CreateAccountAsync(client, bookId, "Main");

        var response = await client.PostAsJsonAsync($"/books/{bookId}/transactions", new TransactionRequest(
            TransactionType.Transfer,
            DateTime.UtcNow.Date,
            100m,
            "USD",
            accountId,
            null,
            null,
            null,
            "Missing destination",
            false));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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
    /// <returns>Created book identifier.</returns>
    private static async Task<Guid> CreateBookAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/books", new CreateBookRequest(name, "USD"));
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
    /// <returns>Created account identifier.</returns>
    private static async Task<Guid> CreateAccountAsync(HttpClient client, Guid bookId, string name)
    {
        var response = await client.PostAsJsonAsync($"/books/{bookId}/accounts", new AccountRequest(
            name,
            name,
            AccountType.Cash,
            "USD",
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
    /// Transaction response payload used in tests.
    /// </summary>
    /// <param name="Id">Transaction identifier.</param>
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
    /// <param name="CreatedAtUtc">Created timestamp in UTC.</param>
    private record TransactionResponse(
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
