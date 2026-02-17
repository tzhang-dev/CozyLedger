namespace CozyLedger.Domain.Entities;

/// <summary>
/// Represents a file attachment linked to a transaction.
/// </summary>
public class Attachment : Entity
{
    /// <summary>
    /// Gets or sets the related transaction identifier.
    /// </summary>
    public Guid TransactionId { get; set; }

    /// <summary>
    /// Gets or sets the original file name provided by the client.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the MIME type of the stored file.
    /// </summary>
    public string MimeType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the relative storage path for the file.
    /// </summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the stored file size in bytes.
    /// </summary>
    public long SizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the original file size in bytes before optional client-side compression.
    /// </summary>
    public long OriginalSizeBytes { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the attachment metadata was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the navigation reference to the related transaction.
    /// </summary>
    public Transaction? Transaction { get; set; }
}
