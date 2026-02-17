namespace CozyLedger.Domain.Entities;

/// <summary>
/// Represents a user's membership in a book.
/// </summary>
public class Membership : Entity
{
    /// <summary>
    /// Gets or sets the related book identifier.
    /// </summary>
    public Guid BookId { get; set; }

    /// <summary>
    /// Gets or sets the related user identifier.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the membership was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the navigation reference to the related book.
    /// </summary>
    public Book? Book { get; set; }
}
