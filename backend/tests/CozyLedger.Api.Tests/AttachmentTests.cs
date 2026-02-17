using System.Net.Http.Headers;
using System.Net.Http.Json;
using CozyLedger.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CozyLedger.Api.Tests;

/// <summary>
/// Verifies attachment upload, listing, and deletion behavior.
/// </summary>
public class AttachmentTests : IClassFixture<PostgresContainerFixture>
{
    private readonly PostgresContainerFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="AttachmentTests"/> class.
    /// </summary>
    /// <param name="fixture">Shared PostgreSQL container fixture.</param>
    public AttachmentTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Verifies that attachment metadata is persisted and removed as files are managed.
    /// </summary>
    /// <returns>A task that completes when assertions are validated.</returns>
    [Fact]
    public async Task Upload_list_and_delete_attachment_persists_metadata()
    {
        await using var factory = new TestWebApplicationFactory(_fixture.ConnectionString);
        await EnsureDatabaseAsync(factory);

        using var client = factory.CreateClient();
        var token = await RegisterAndGetToken(client, "attach@cozy.local");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var bookId = await CreateBookAsync(client, "Attachment Book");
        var accountId = await CreateAccountAsync(client, bookId, "Cash");
        var categoryId = await CreateCategoryAsync(client, bookId, "Receipts", CategoryType.Expense);
        var transactionId = await CreateTransactionAsync(client, bookId, accountId, categoryId);

        var content = new MultipartFormDataContent();
        var fileBytes = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 13 };
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "file", "receipt.png");
        content.Add(new StringContent("2048"), "originalSizeBytes");

        var uploadResponse = await client.PostAsync($"/books/{bookId}/transactions/{transactionId}/attachments", content);
        uploadResponse.IsSuccessStatusCode.Should().BeTrue();
        var attachment = await uploadResponse.Content.ReadFromJsonAsync<AttachmentResponse>();
        attachment!.MimeType.Should().Be("image/png");
        attachment.OriginalSizeBytes.Should().Be(2048);

        var listResponse = await client.GetAsync($"/books/{bookId}/transactions/{transactionId}/attachments");
        listResponse.IsSuccessStatusCode.Should().BeTrue();
        var list = await listResponse.Content.ReadFromJsonAsync<List<AttachmentResponse>>();
        list.Should().NotBeNull();
        list!.Count.Should().Be(1);
        list[0].Id.Should().Be(attachment.Id);

        var deleteResponse = await client.DeleteAsync($"/books/{bookId}/transactions/{transactionId}/attachments/{attachment.Id}");
        deleteResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);

        var afterDelete = await client.GetAsync($"/books/{bookId}/transactions/{transactionId}/attachments");
        var afterList = await afterDelete.Content.ReadFromJsonAsync<List<AttachmentResponse>>();
        afterList.Should().NotBeNull();
        afterList!.Count.Should().Be(0);
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
    /// Creates a transaction in the specified book and returns its identifier.
    /// </summary>
    /// <param name="client">HTTP client targeting the test server.</param>
    /// <param name="bookId">Book identifier.</param>
    /// <param name="accountId">Account identifier.</param>
    /// <param name="categoryId">Category identifier.</param>
    /// <returns>Created transaction identifier.</returns>
    private static async Task<Guid> CreateTransactionAsync(HttpClient client, Guid bookId, Guid accountId, Guid categoryId)
    {
        var response = await client.PostAsJsonAsync($"/books/{bookId}/transactions", new TransactionRequest(
            TransactionType.Expense,
            DateTime.UtcNow.Date,
            10m,
            "USD",
            accountId,
            null,
            categoryId,
            null,
            "Receipt",
            false));
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<TransactionResponse>();
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
        var dbContext = scope.ServiceProvider.GetRequiredService<CozyLedger.Infrastructure.Data.AppDbContext>();
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

    /// <summary>
    /// Attachment response payload used in tests.
    /// </summary>
    /// <param name="Id">Attachment identifier.</param>
    /// <param name="FileName">File name.</param>
    /// <param name="MimeType">MIME type.</param>
    /// <param name="SizeBytes">Stored size in bytes.</param>
    /// <param name="OriginalSizeBytes">Original size in bytes.</param>
    /// <param name="CreatedAtUtc">Created timestamp in UTC.</param>
    private record AttachmentResponse(
        Guid Id,
        string FileName,
        string MimeType,
        long SizeBytes,
        long OriginalSizeBytes,
        DateTime CreatedAtUtc);
}
