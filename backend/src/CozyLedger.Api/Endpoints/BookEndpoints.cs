using System.Security.Claims;
using CozyLedger.Api.Extensions;
using CozyLedger.Domain.Entities;
using CozyLedger.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CozyLedger.Api.Endpoints;

public static class BookEndpoints
{
    public static IEndpointRouteBuilder MapBookEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/books").RequireAuthorization();

        group.MapPost("/", CreateBookAsync);

        return app;
    }

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

    public record CreateBookRequest(string Name, string? BaseCurrency);

    public record BookResponse(Guid Id, string Name, string BaseCurrency);
}
