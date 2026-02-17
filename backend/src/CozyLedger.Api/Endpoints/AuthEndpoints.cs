using CozyLedger.Api.Services;
using CozyLedger.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace CozyLedger.Api.Endpoints;

/// <summary>
/// Defines authentication-related API endpoints.
/// </summary>
public static class AuthEndpoints
{
    /// <summary>
    /// Maps authentication endpoints onto the route builder.
    /// </summary>
    /// <param name="app">Route builder to configure.</param>
    /// <returns>The original route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth");

        group.MapPost("/register", RegisterAsync);
        group.MapPost("/login", LoginAsync);

        return app;
    }

    /// <summary>
    /// Registers a new user account and returns an access token.
    /// </summary>
    /// <param name="request">Registration payload.</param>
    /// <param name="userManager">ASP.NET Core user manager.</param>
    /// <param name="tokenService">Service used to generate JWT tokens.</param>
    /// <returns>HTTP result containing registration outcome and token when successful.</returns>
    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        UserManager<ApplicationUser> userManager,
        JwtTokenService tokenService)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(new { error = "Email and password are required." });
        }

        var existing = await userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
        {
            return Results.BadRequest(new { error = "User already exists." });
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName ?? request.Email,
            Locale = request.Locale ?? "en"
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return Results.BadRequest(new { error = string.Join("; ", createResult.Errors.Select(e => e.Description)) });
        }

        var token = tokenService.CreateToken(user);
        return Results.Ok(new AuthResponse(token.Token, token.ExpiresAtUtc));
    }

    /// <summary>
    /// Authenticates an existing user and returns a new access token.
    /// </summary>
    /// <param name="request">Login payload.</param>
    /// <param name="userManager">ASP.NET Core user manager.</param>
    /// <param name="tokenService">Service used to generate JWT tokens.</param>
    /// <returns>HTTP result containing an authentication token when credentials are valid.</returns>
    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        UserManager<ApplicationUser> userManager,
        JwtTokenService tokenService)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(new { error = "Email and password are required." });
        }

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var valid = await userManager.CheckPasswordAsync(user, request.Password);
        if (!valid)
        {
            return Results.Unauthorized();
        }

        var token = tokenService.CreateToken(user);
        return Results.Ok(new AuthResponse(token.Token, token.ExpiresAtUtc));
    }

    /// <summary>
    /// Represents payload for user registration.
    /// </summary>
    /// <param name="Email">User email address.</param>
    /// <param name="Password">Plain-text password submitted for account creation.</param>
    /// <param name="DisplayName">Optional display name.</param>
    /// <param name="Locale">Optional locale code.</param>
    public record RegisterRequest(string Email, string Password, string? DisplayName, string? Locale);

    /// <summary>
    /// Represents payload for user login.
    /// </summary>
    /// <param name="Email">User email address.</param>
    /// <param name="Password">Plain-text password.</param>
    public record LoginRequest(string Email, string Password);

    /// <summary>
    /// Represents a successful authentication response.
    /// </summary>
    /// <param name="Token">JWT bearer token.</param>
    /// <param name="ExpiresAtUtc">UTC expiration timestamp.</param>
    public record AuthResponse(string Token, DateTime ExpiresAtUtc);
}
