namespace CozyLedger.Domain.Entities;

/// <summary>
/// Represents the many-to-many association between a transaction and a tag.
/// </summary>
public class TransactionTag
{
    /// <summary>
    /// Gets or sets the transaction identifier.
    /// </summary>
    public Guid TransactionId { get; set; }

    /// <summary>
    /// Gets or sets the tag identifier.
    /// </summary>
    public Guid TagId { get; set; }

    /// <summary>
    /// Gets or sets the navigation reference to the transaction.
    /// </summary>
    public Transaction? Transaction { get; set; }

    /// <summary>
    /// Gets or sets the navigation reference to the tag.
    /// </summary>
    public Tag? Tag { get; set; }
}
