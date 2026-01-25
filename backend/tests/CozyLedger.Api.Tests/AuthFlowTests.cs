using System.Net.Http.Headers;
using System.Net.Http.Json;
using CozyLedger.Infrastructure.Data;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CozyLedger.Api.Tests;

public class AuthFlowTests : IClassFixture<PostgresContainerFixture>
{
    private readonly PostgresContainerFixture _fixture;

    public AuthFlowTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Register_and_login_return_tokens()
    {
        await using var factory = new TestWebApplicationFactory(_fixture.ConnectionString);
        await EnsureDatabaseAsync(factory);

        using var client = factory.CreateClient();

        var registerResponse = await client.PostAsJsonAsync("/auth/register", new RegisterRequest(
            "owner@cozy.local",
            "Password123",
            "Owner",
            "en"));

        registerResponse.IsSuccessStatusCode.Should().BeTrue();
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        registerResult!.Token.Should().NotBeNullOrWhiteSpace();

        var loginResponse = await client.PostAsJsonAsync("/auth/login", new LoginRequest(
            "owner@cozy.local",
            "Password123"));

        loginResponse.IsSuccessStatusCode.Should().BeTrue();
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        loginResult!.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Invite_flow_allows_member_join_and_rejects_invalid_token()
    {
        await using var factory = new TestWebApplicationFactory(_fixture.ConnectionString);
        await EnsureDatabaseAsync(factory);

        using var ownerClient = factory.CreateClient();
        var ownerToken = await RegisterAndGetToken(ownerClient, "owner2@cozy.local");
        ownerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);

        var bookResponse = await ownerClient.PostAsJsonAsync("/books", new CreateBookRequest(
            "Household",
            "USD"));
        bookResponse.IsSuccessStatusCode.Should().BeTrue();
        var book = await bookResponse.Content.ReadFromJsonAsync<BookResponse>();
        book.Should().NotBeNull();

        var inviteResponse = await ownerClient.PostAsJsonAsync($"/books/{book!.Id}/invites", new { });
        inviteResponse.IsSuccessStatusCode.Should().BeTrue();
        var invite = await inviteResponse.Content.ReadFromJsonAsync<InviteResponse>();
        invite.Should().NotBeNull();

        using var memberClient = factory.CreateClient();
        var memberToken = await RegisterAndGetToken(memberClient, "member@cozy.local");
        memberClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);

        var acceptResponse = await memberClient.PostAsync($"/invites/{invite!.Token}/accept", null);
        acceptResponse.IsSuccessStatusCode.Should().BeTrue();

        var invalidResponse = await memberClient.PostAsync("/invites/not-a-token/accept", null);
        invalidResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
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

    private static async Task EnsureDatabaseAsync(TestWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }

    private record RegisterRequest(string Email, string Password, string? DisplayName, string? Locale);

    private record LoginRequest(string Email, string Password);

    private record AuthResponse(string Token, DateTime ExpiresAtUtc);

    private record CreateBookRequest(string Name, string? BaseCurrency);

    private record BookResponse(Guid Id, string Name, string BaseCurrency);

    private record InviteResponse(string Token, string InviteUrl, DateTime ExpiresAtUtc);
}
