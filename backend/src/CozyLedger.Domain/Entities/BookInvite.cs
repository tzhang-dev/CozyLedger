namespace CozyLedger.Domain.Entities;

/// <summary>
/// Represents an invitation token that grants access to a book.
/// </summary>
public class BookInvite : Entity
{
    /// <summary>
    /// Gets or sets the target book identifier.
    /// </summary>
    public Guid BookId { get; set; }

    /// <summary>
    /// Gets or sets the invitation token value.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC expiration timestamp for the invitation.
    /// </summary>
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the creator user identifier.
    /// </summary>
    public Guid CreatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the invitation was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets a value indicating whether the invitation has been accepted.
    /// </summary>
    public bool IsUsed { get; set; }

    /// <summary>
    /// Gets or sets the user identifier that accepted the invitation.
    /// </summary>
    public Guid? UsedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the invitation was accepted.
    /// </summary>
    public DateTime? UsedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the navigation reference to the invited book.
    /// </summary>
    public Book? Book { get; set; }
}
