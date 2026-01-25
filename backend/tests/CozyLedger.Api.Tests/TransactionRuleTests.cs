using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CozyLedger.Domain.Enums;
using CozyLedger.Infrastructure.Data;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CozyLedger.Api.Tests;

public class TransactionRuleTests : IClassFixture<PostgresContainerFixture>
{
    private readonly PostgresContainerFixture _fixture;

    public TransactionRuleTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

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

    private static async Task<Guid> CreateBookAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/books", new CreateBookRequest(name, "USD"));
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<BookResponse>();
        return result!.Id;
    }

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
