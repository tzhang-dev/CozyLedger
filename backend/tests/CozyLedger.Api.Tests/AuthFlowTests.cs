using System.Net.Http.Headers;
using System.Net.Http.Json;
using CozyLedger.Infrastructure.Data;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CozyLedger.Api.Tests;

/// <summary>
/// Covers registration, login, and invite acceptance authentication flows.
/// </summary>
public class AuthFlowTests : IClassFixture<PostgresContainerFixture>
{
    private readonly PostgresContainerFixture _fixture;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthFlowTests"/> class.
    /// </summary>
    /// <param name="fixture">Shared PostgreSQL container fixture.</param>
    public AuthFlowTests(PostgresContainerFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Verifies that registration and login both return JWT tokens.
    /// </summary>
    /// <returns>A task that completes when assertions are validated.</returns>
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

    /// <summary>
    /// Verifies invite creation and acceptance flow, including invalid token handling.
    /// </summary>
    /// <returns>A task that completes when assertions are validated.</returns>
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
    /// Login request payload used in tests.
    /// </summary>
    /// <param name="Email">User email address.</param>
    /// <param name="Password">Plain-text password.</param>
    private record LoginRequest(string Email, string Password);

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
    /// Invite response payload used in tests.
    /// </summary>
    /// <param name="Token">Invite token.</param>
    /// <param name="InviteUrl">Invite URL.</param>
    /// <param name="ExpiresAtUtc">UTC expiration timestamp.</param>
    private record InviteResponse(string Token, string InviteUrl, DateTime ExpiresAtUtc);
}
