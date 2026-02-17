namespace CozyLedger.Domain.Entities;

/// <summary>
/// Represents a reusable tag that can be attached to transactions.
/// </summary>
public class Tag : Entity
{
    /// <summary>
    /// Gets or sets the owning book identifier.
    /// </summary>
    public Guid BookId { get; set; }

    /// <summary>
    /// Gets or sets the English tag name.
    /// </summary>
    public string NameEn { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Simplified Chinese tag name.
    /// </summary>
    public string NameZhHans { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the navigation reference to the owning book.
    /// </summary>
    public Book? Book { get; set; }

    /// <summary>
    /// Gets or sets the transaction associations for this tag.
    /// </summary>
    public ICollection<TransactionTag> TransactionTags { get; set; } = new List<TransactionTag>();
}
