namespace CozyLedger.Domain.Entities;

/// <summary>
/// Defines the common identifier field shared by persisted domain entities.
/// </summary>
public abstract class Entity
{
    /// <summary>
    /// Gets or sets the unique identifier for the entity.
    /// </summary>
    public Guid Id { get; set; }
}
