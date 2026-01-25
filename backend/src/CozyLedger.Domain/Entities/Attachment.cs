namespace CozyLedger.Domain.Entities;

public class Attachment : Entity
{
    public Guid TransactionId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public long OriginalSizeBytes { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Transaction? Transaction { get; set; }
}