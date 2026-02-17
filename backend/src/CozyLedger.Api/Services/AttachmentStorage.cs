using CozyLedger.Api.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CozyLedger.Api.Services;

/// <summary>
/// Provides file-system storage operations for transaction attachments.
/// </summary>
public class AttachmentStorage
{
    private readonly AttachmentStorageOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="AttachmentStorage"/> class.
    /// </summary>
    /// <param name="options">Attachment storage configuration values.</param>
    public AttachmentStorage(IOptions<AttachmentStorageOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>
    /// Persists an uploaded file for a transaction and returns storage metadata.
    /// </summary>
    /// <param name="transactionId">Transaction identifier used to scope storage location.</param>
    /// <param name="file">Uploaded file to persist.</param>
    /// <param name="cancellationToken">Cancellation token for the async write operation.</param>
    /// <returns>Stored attachment metadata.</returns>
    public async Task<StoredAttachment> SaveAsync(Guid transactionId, IFormFile file, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var relativePath = Path.Combine(transactionId.ToString(), fileName);
        var fullPath = Path.Combine(_options.RootPath, relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var stream = File.Create(fullPath);
        await file.CopyToAsync(stream, cancellationToken);

        return new StoredAttachment(relativePath, fileName, file.Length);
    }

    /// <summary>
    /// Deletes a previously stored file when it exists.
    /// </summary>
    /// <param name="relativePath">Relative file path returned by <see cref="SaveAsync"/>.</param>
    public void Delete(string relativePath)
    {
        var fullPath = Path.Combine(_options.RootPath, relativePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }
}

/// <summary>
/// Represents metadata about an attachment file stored on disk.
/// </summary>
/// <param name="RelativePath">Relative path under the configured storage root.</param>
/// <param name="FileName">Generated file name used in storage.</param>
/// <param name="SizeBytes">Stored file size in bytes.</param>
public record StoredAttachment(string RelativePath, string FileName, long SizeBytes);
