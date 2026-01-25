using CozyLedger.Api.Services;
using CozyLedger.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace CozyLedger.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth");

        group.MapPost("/register", RegisterAsync);
        group.MapPost("/login", LoginAsync);

        return app;
    }

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

    public record RegisterRequest(string Email, string Password, string? DisplayName, string? Locale);

    public record LoginRequest(string Email, string Password);

    public record AuthResponse(string Token, DateTime ExpiresAtUtc);
}