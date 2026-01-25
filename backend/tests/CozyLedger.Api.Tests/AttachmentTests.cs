using System.Net.Http.Headers;
using System.Net.Http.Json;
using CozyLedger.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CozyLedger.Api.Tests;

public class AttachmentTests : IClassFixture<PostgresContainerFixture>
{
    private readonly PostgresContainerFixture _fixture;

    public AttachmentTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

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

    private static async Task EnsureDatabaseAsync(TestWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CozyLedger.Infrastructure.Data.AppDbContext>();
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

    private record AttachmentResponse(
        Guid Id,
        string FileName,
        string MimeType,
        long SizeBytes,
        long OriginalSizeBytes,
        DateTime CreatedAtUtc);
}