namespace CozyLedger.Domain.Entities;

/// <summary>
/// Represents a ledger book that groups members, accounts, and transactions.
/// </summary>
public class Book : Entity
{
    /// <summary>
    /// Gets or sets the display name of the book.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ISO 4217 base currency used for reporting in this book.
    /// </summary>
    public string BaseCurrency { get; set; } = "USD";

    /// <summary>
    /// Gets or sets the UTC timestamp when the book was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets memberships that grant users access to this book.
    /// </summary>
    public ICollection<Membership> Memberships { get; set; } = new List<Membership>();
}
