using System.Security.Claims;
using CozyLedger.Api.Extensions;
using CozyLedger.Domain.Entities;
using CozyLedger.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CozyLedger.Api.Endpoints;

/// <summary>
/// Defines book management API endpoints.
/// </summary>
public static class BookEndpoints
{
    /// <summary>
    /// Maps book endpoints onto the route builder.
    /// </summary>
    /// <param name="app">Route builder to configure.</param>
    /// <returns>The original route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapBookEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/books").RequireAuthorization();

        group.MapPost("/", CreateBookAsync);

        return app;
    }

    /// <summary>
    /// Creates a new book and assigns the requesting user as a member.
    /// </summary>
    /// <param name="request">Book creation payload.</param>
    /// <param name="user">Authenticated user principal.</param>
    /// <param name="dbContext">Database context.</param>
    /// <returns>HTTP result containing the created book metadata.</returns>
    private static async Task<IResult> CreateBookAsync(
        CreateBookRequest request,
        ClaimsPrincipal user,
        AppDbContext dbContext)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.BadRequest(new { error = "Book name is required." });
        }

        var userId = user.GetUserId();
        var book = new Book
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            BaseCurrency = string.IsNullOrWhiteSpace(request.BaseCurrency) ? "USD" : request.BaseCurrency
        };

        var membership = new Membership
        {
            Id = Guid.NewGuid(),
            BookId = book.Id,
            UserId = userId
        };

        dbContext.Books.Add(book);
        dbContext.Memberships.Add(membership);
        await dbContext.SaveChangesAsync();

        return Results.Created($"/books/{book.Id}", new BookResponse(book.Id, book.Name, book.BaseCurrency));
    }

    /// <summary>
    /// Represents payload for creating a new book.
    /// </summary>
    /// <param name="Name">Book name.</param>
    /// <param name="BaseCurrency">Optional ISO 4217 base currency.</param>
    public record CreateBookRequest(string Name, string? BaseCurrency);

    /// <summary>
    /// Represents book details returned by the API.
    /// </summary>
    /// <param name="Id">Book identifier.</param>
    /// <param name="Name">Book name.</param>
    /// <param name="BaseCurrency">Book base currency code.</param>
    public record BookResponse(Guid Id, string Name, string BaseCurrency);
}
