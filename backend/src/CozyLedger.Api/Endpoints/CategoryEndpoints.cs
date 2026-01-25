using System.Security.Claims;
using CozyLedger.Api.Extensions;
using CozyLedger.Domain.Entities;
using CozyLedger.Domain.Enums;
using CozyLedger.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CozyLedger.Api.Endpoints;

public static class CategoryEndpoints
{
    public static IEndpointRouteBuilder MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/books/{bookId:guid}/categories").RequireAuthorization();

        group.MapGet("/", ListCategoriesAsync);
        group.MapPost("/", CreateCategoryAsync);
        group.MapPut("/{categoryId:guid}", UpdateCategoryAsync);

        return app;
    }

    private static async Task<IResult> ListCategoriesAsync(Guid bookId, ClaimsPrincipal user, AppDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (!await IsMemberAsync(dbContext, bookId, userId))
        {
            return Results.Forbid();
        }

        var categories = await dbContext.Categories
            .AsNoTracking()
            .Where(c => c.BookId == bookId)
            .OrderBy(c => c.NameEn)
            .Select(c => new CategoryResponse(
                c.Id,
                c.NameEn,
                c.NameZhHans,
                c.Type,
                c.ParentId,
                c.IsActive))
            .ToListAsync();

        return Results.Ok(categories);
    }

    private static async Task<IResult> CreateCategoryAsync(
        Guid bookId,
        CreateCategoryRequest request,
        ClaimsPrincipal user,
        AppDbContext dbContext)
    {
        if (string.IsNullOrWhiteSpace(request.NameEn) || string.IsNullOrWhiteSpace(request.NameZhHans))
        {
            return Results.BadRequest(new { error = "Category names are required." });
        }

        var userId = user.GetUserId();
        if (!await IsMemberAsync(dbContext, bookId, userId))
        {
            return Results.Forbid();
        }

        if (request.ParentId.HasValue)
        {
            var parentExists = await dbContext.Categories.AnyAsync(c => c.Id == request.ParentId && c.BookId == bookId);
            if (!parentExists)
            {
                return Results.BadRequest(new { error = "Parent category not found." });
            }
        }

        var category = new Category
        {
            Id = Guid.NewGuid(),
            BookId = bookId,
            NameEn = request.NameEn,
            NameZhHans = request.NameZhHans,
            Type = request.Type,
            ParentId = request.ParentId,
            IsActive = request.IsActive
        };

        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync();

        return Results.Created($"/books/{bookId}/categories/{category.Id}", new CategoryResponse(
            category.Id,
            category.NameEn,
            category.NameZhHans,
            category.Type,
            category.ParentId,
            category.IsActive));
    }

    private static async Task<IResult> UpdateCategoryAsync(
        Guid bookId,
        Guid categoryId,
        UpdateCategoryRequest request,
        ClaimsPrincipal user,
        AppDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (!await IsMemberAsync(dbContext, bookId, userId))
        {
            return Results.Forbid();
        }

        var category = await dbContext.Categories.FirstOrDefaultAsync(c => c.Id == categoryId && c.BookId == bookId);
        if (category is null)
        {
            return Results.NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.NameEn) || string.IsNullOrWhiteSpace(request.NameZhHans))
        {
            return Results.BadRequest(new { error = "Category names are required." });
        }

        if (request.ParentId.HasValue && request.ParentId == categoryId)
        {
            return Results.BadRequest(new { error = "Category cannot be its own parent." });
        }

        if (request.ParentId.HasValue)
        {
            var parentExists = await dbContext.Categories.AnyAsync(c => c.Id == request.ParentId && c.BookId == bookId);
            if (!parentExists)
            {
                return Results.BadRequest(new { error = "Parent category not found." });
            }
        }

        category.NameEn = request.NameEn;
        category.NameZhHans = request.NameZhHans;
        category.Type = request.Type;
        category.ParentId = request.ParentId;
        category.IsActive = request.IsActive;

        await dbContext.SaveChangesAsync();

        return Results.Ok(new CategoryResponse(
            category.Id,
            category.NameEn,
            category.NameZhHans,
            category.Type,
            category.ParentId,
            category.IsActive));
    }

    private static Task<bool> IsMemberAsync(AppDbContext dbContext, Guid bookId, Guid userId)
        => dbContext.Memberships.AnyAsync(m => m.BookId == bookId && m.UserId == userId);

    public record CreateCategoryRequest(
        string NameEn,
        string NameZhHans,
        CategoryType Type,
        Guid? ParentId,
        bool IsActive);

    public record UpdateCategoryRequest(
        string NameEn,
        string NameZhHans,
        CategoryType Type,
        Guid? ParentId,
        bool IsActive);

    public record CategoryResponse(
        Guid Id,
        string NameEn,
        string NameZhHans,
        CategoryType Type,
        Guid? ParentId,
        bool IsActive);
}