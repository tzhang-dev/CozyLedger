using CozyLedger.Api.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CozyLedger.Api.Services;

public class AttachmentStorage
{
    private readonly AttachmentStorageOptions _options;

    public AttachmentStorage(IOptions<AttachmentStorageOptions> options)
    {
        _options = options.Value;
    }

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

    public void Delete(string relativePath)
    {
        var fullPath = Path.Combine(_options.RootPath, relativePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
    }
}

public record StoredAttachment(string RelativePath, string FileName, long SizeBytes);
