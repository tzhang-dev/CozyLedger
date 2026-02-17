using CozyLedger.Domain.Enums;

namespace CozyLedger.Domain.Entities;

/// <summary>
/// Represents a ledger transaction within a book.
/// </summary>
public class Transaction : Entity
{
    /// <summary>
    /// Gets or sets the owning book identifier.
    /// </summary>
    public Guid BookId { get; set; }

    /// <summary>
    /// Gets or sets the transaction kind.
    /// </summary>
    public TransactionType Type { get; set; }

    /// <summary>
    /// Gets or sets the transaction date and time in UTC.
    /// </summary>
    public DateTime DateUtc { get; set; }

    /// <summary>
    /// Gets or sets the transaction amount in <see cref="Currency"/>.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the ISO 4217 transaction currency code.
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Gets or sets the source account identifier.
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// Gets or sets the destination account identifier for transfers.
    /// </summary>
    public Guid? ToAccountId { get; set; }

    /// <summary>
    /// Gets or sets the category identifier for income and expense transactions.
    /// </summary>
    public Guid? CategoryId { get; set; }

    /// <summary>
    /// Gets or sets the optional member identifier associated with this transaction.
    /// </summary>
    public Guid? MemberId { get; set; }

    /// <summary>
    /// Gets or sets an optional note.
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this transaction is a refund.
    /// </summary>
    public bool IsRefund { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who created the transaction.
    /// </summary>
    public Guid CreatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the transaction was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the navigation reference to the owning book.
    /// </summary>
    public Book? Book { get; set; }

    /// <summary>
    /// Gets or sets the navigation reference to the source account.
    /// </summary>
    public Account? Account { get; set; }

    /// <summary>
    /// Gets or sets the navigation reference to the destination account.
    /// </summary>
    public Account? ToAccount { get; set; }

    /// <summary>
    /// Gets or sets the navigation reference to the category.
    /// </summary>
    public Category? Category { get; set; }

    /// <summary>
    /// Gets or sets tags attached to this transaction.
    /// </summary>
    public ICollection<TransactionTag> TransactionTags { get; set; } = new List<TransactionTag>();

    /// <summary>
    /// Gets or sets attachments associated with this transaction.
    /// </summary>
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
}
