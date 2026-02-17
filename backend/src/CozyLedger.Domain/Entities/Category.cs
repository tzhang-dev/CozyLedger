using CozyLedger.Domain.Enums;

namespace CozyLedger.Domain.Entities;

/// <summary>
/// Represents a transaction category for income or expense classification.
/// </summary>
public class Category : Entity
{
    /// <summary>
    /// Gets or sets the owning book identifier.
    /// </summary>
    public Guid BookId { get; set; }

    /// <summary>
    /// Gets or sets the optional parent category identifier.
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// Gets or sets the English category name.
    /// </summary>
    public string NameEn { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Simplified Chinese category name.
    /// </summary>
    public string NameZhHans { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the category type.
    /// </summary>
    public CategoryType Type { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this category is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the navigation reference to the owning book.
    /// </summary>
    public Book? Book { get; set; }

    /// <summary>
    /// Gets or sets the navigation reference to the parent category.
    /// </summary>
    public Category? Parent { get; set; }

    /// <summary>
    /// Gets or sets child categories under this category.
    /// </summary>
    public ICollection<Category> Children { get; set; } = new List<Category>();
}
