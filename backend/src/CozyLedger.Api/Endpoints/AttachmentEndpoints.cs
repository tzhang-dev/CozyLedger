using System.Security.Claims;
using CozyLedger.Api.Extensions;
using CozyLedger.Api.Services;
using CozyLedger.Domain.Entities;
using CozyLedger.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CozyLedger.Api.Endpoints;

/// <summary>
/// Defines attachment management API endpoints.
/// </summary>
public static class AttachmentEndpoints
{
    private const long MaxImageBytes = 1 * 1024 * 1024;
    private const long MaxPdfBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "application/pdf"
    };

    /// <summary>
    /// Maps attachment endpoints onto the route builder.
    /// </summary>
    /// <param name="app">Route builder to configure.</param>
    /// <returns>The original route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapAttachmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/books/{bookId:guid}/transactions/{transactionId:guid}/attachments")
            .RequireAuthorization();

        group.MapGet("/", ListAttachmentsAsync);
        group.MapPost("/", UploadAttachmentAsync);
        group.MapDelete("/{attachmentId:guid}", DeleteAttachmentAsync);

        return app;
    }

    /// <summary>
    /// Lists attachments associated with a transaction.
    /// </summary>
    /// <param name="bookId">Book identifier.</param>
    /// <param name="transactionId">Transaction identifier.</param>
    /// <param name="user">Authenticated user principal.</param>
    /// <param name="dbContext">Database context.</param>
    /// <returns>HTTP result containing attachment records.</returns>
    private static async Task<IResult> ListAttachmentsAsync(
        Guid bookId,
        Guid transactionId,
        ClaimsPrincipal user,
        AppDbContext dbContext)
    {
        var userId = user.GetUserId();
        if (!await IsMemberAsync(dbContext, bookId, userId))
        {
            return Results.Forbid();
        }

        var transactionExists = await dbContext.Transactions.AnyAsync(t => t.Id == transactionId && t.BookId == bookId);
        if (!transactionExists)
        {
            return Results.NotFound();
        }

        var attachments = await dbContext.Attachments
            .AsNoTracking()
            .Where(a => a.TransactionId == transactionId)
            .OrderByDescending(a => a.CreatedAtUtc)
            .Select(a => new AttachmentResponse(
                a.Id,
                a.FileName,
                a.MimeType,
                a.SizeBytes,
                a.OriginalSizeBytes,
                a.CreatedAtUtc))
            .ToListAsync();

        return Results.Ok(attachments);
    }

    /// <summary>
    /// Uploads an attachment for a transaction and stores file metadata.
    /// </summary>
    /// <param name="bookId">Book identifier.</param>
    /// <param name="transactionId">Transaction identifier.</param>
    /// <param name="user">Authenticated user principal.</param>
    /// <param name="dbContext">Database context.</param>
    /// <param name="storage">Attachment storage service.</param>
    /// <param name="request">Incoming HTTP request containing multipart form data.</param>
    /// <param name="cancellationToken">Cancellation token for async I/O operations.</param>
    /// <returns>HTTP result containing the created attachment metadata.</returns>
    private static async Task<IResult> UploadAttachmentAsync(
        Guid bookId,
        Guid transactionId,
        ClaimsPrincipal user,
        AppDbContext dbContext,
        AttachmentStorage storage,
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        var userId = user.GetUserId();
        if (!await IsMemberAsync(dbContext, bookId, userId))
        {
            return Results.Forbid();
        }

        var transactionExists = await dbContext.Transactions.AnyAsync(t => t.Id == transactionId && t.BookId == bookId);
        if (!transactionExists)
        {
            return Results.NotFound();
        }

        if (!request.HasFormContentType)
        {
            return Results.BadRequest(new { error = "Multipart form data is required." });
        }

        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files.FirstOrDefault();
        if (file is null || file.Length == 0)
        {
            return Results.BadRequest(new { error = "File is required." });
        }

        if (!AllowedTypes.Contains(file.ContentType))
        {
            return Results.BadRequest(new { error = "Unsupported file type." });
        }

        if (file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) && file.Length > MaxImageBytes)
        {
            return Results.BadRequest(new { error = "Image must be <= 1MB after compression." });
        }

        if (file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) && file.Length > MaxPdfBytes)
        {
            return Results.BadRequest(new { error = "PDF must be <= 5MB." });
        }

        var originalSizeBytes = file.Length;
        if (form.TryGetValue("originalSizeBytes", out var originalSizeValue)
            && long.TryParse(originalSizeValue.ToString(), out var parsedSize))
        {
            originalSizeBytes = parsedSize;
        }

        var stored = await storage.SaveAsync(transactionId, file, cancellationToken);
        var attachment = new Attachment
        {
            Id = Guid.NewGuid(),
            TransactionId = transactionId,
            FileName = file.FileName,
            MimeType = file.ContentType,
            StoragePath = stored.RelativePath.Replace('\\', '/'),
            SizeBytes = stored.SizeBytes,
            OriginalSizeBytes = originalSizeBytes
        };

        dbContext.Attachments.Add(attachment);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Created($"/books/{bookId}/transactions/{transactionId}/attachments/{attachment.Id}", new AttachmentResponse(
            attachment.Id,
            attachment.FileName,
            attachment.MimeType,
            attachment.SizeBytes,
            attachment.OriginalSizeBytes,
            attachment.CreatedAtUtc));
    }

    /// <summary>
    /// Deletes an attachment and removes the underlying file from storage.
    /// </summary>
    /// <param name="bookId">Book identifier.</param>
    /// <param name="transactionId">Transaction identifier.</param>
    /// <param name="attachmentId">Attachment identifier.</param>
    /// <param name="user">Authenticated user principal.</param>
    /// <param name="dbContext">Database context.</param>
    /// <param name="storage">Attachment storage service.</param>
    /// <returns>HTTP result indicating deletion outcome.</returns>
    private static async Task<IResult> DeleteAttachmentAsync(
        Guid bookId,
        Guid transactionId,
        Guid attachmentId,
        ClaimsPrincipal user,
        AppDbContext dbContext,
        AttachmentStorage storage)
    {
        var userId = user.GetUserId();
        if (!await IsMemberAsync(dbContext, bookId, userId))
        {
            return Results.Forbid();
        }

        var attachment = await dbContext.Attachments
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.TransactionId == transactionId);

        if (attachment is null)
        {
            return Results.NotFound();
        }

        dbContext.Attachments.Remove(attachment);
        await dbContext.SaveChangesAsync();

        storage.Delete(attachment.StoragePath);

        return Results.NoContent();
    }

    /// <summary>
    /// Determines whether a user is a member of the specified book.
    /// </summary>
    /// <param name="dbContext">Database context.</param>
    /// <param name="bookId">Book identifier.</param>
    /// <param name="userId">User identifier.</param>
    /// <returns><see langword="true"/> when membership exists; otherwise, <see langword="false"/>.</returns>
    private static Task<bool> IsMemberAsync(AppDbContext dbContext, Guid bookId, Guid userId)
        => dbContext.Memberships.AnyAsync(m => m.BookId == bookId && m.UserId == userId);

    /// <summary>
    /// Represents attachment data returned by the API.
    /// </summary>
    /// <param name="Id">Attachment identifier.</param>
    /// <param name="FileName">Original file name.</param>
    /// <param name="MimeType">MIME type.</param>
    /// <param name="SizeBytes">Stored file size in bytes.</param>
    /// <param name="OriginalSizeBytes">Original file size in bytes.</param>
    /// <param name="CreatedAtUtc">Creation timestamp in UTC.</param>
    public record AttachmentResponse(
        Guid Id,
        string FileName,
        string MimeType,
        long SizeBytes,
        long OriginalSizeBytes,
        DateTime CreatedAtUtc);
}
