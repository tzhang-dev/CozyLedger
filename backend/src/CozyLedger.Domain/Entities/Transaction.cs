using CozyLedger.Domain.Enums;

namespace CozyLedger.Domain.Entities;

public class Transaction : Entity
{
    public Guid BookId { get; set; }
    public TransactionType Type { get; set; }
    public DateTime DateUtc { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public Guid AccountId { get; set; }
    public Guid? ToAccountId { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? MemberId { get; set; }
    public string? Note { get; set; }
    public bool IsRefund { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Book? Book { get; set; }
    public Account? Account { get; set; }
    public Account? ToAccount { get; set; }
    public Category? Category { get; set; }
    public ICollection<TransactionTag> TransactionTags { get; set; } = new List<TransactionTag>();
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
}