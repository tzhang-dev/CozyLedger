using CozyLedger.Domain.Enums;

namespace CozyLedger.Domain.Entities;

/// <summary>
/// Represents a financial account inside a book.
/// </summary>
public class Account : Entity
{
    /// <summary>
    /// Gets or sets the owning book identifier.
    /// </summary>
    public Guid BookId { get; set; }

    /// <summary>
    /// Gets or sets the English account name.
    /// </summary>
    public string NameEn { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Simplified Chinese account name.
    /// </summary>
    public string NameZhHans { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the account classification.
    /// </summary>
    public AccountType Type { get; set; }

    /// <summary>
    /// Gets or sets the ISO 4217 currency code used by this account.
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Gets or sets a value indicating whether the account should be hidden in standard views.
    /// </summary>
    public bool IsHidden { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the account contributes to net worth calculations.
    /// </summary>
    public bool IncludeInNetWorth { get; set; } = true;

    /// <summary>
    /// Gets or sets an optional free-form note for the account.
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the account was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the navigation reference to the owning book.
    /// </summary>
    public Book? Book { get; set; }
}
