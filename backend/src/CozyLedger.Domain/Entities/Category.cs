using CozyLedger.Domain.Enums;

namespace CozyLedger.Domain.Entities;

public class Category : Entity
{
    public Guid BookId { get; set; }
    public Guid? ParentId { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string NameZhHans { get; set; } = string.Empty;
    public CategoryType Type { get; set; }
    public bool IsActive { get; set; } = true;

    public Book? Book { get; set; }
    public Category? Parent { get; set; }
    public ICollection<Category> Children { get; set; } = new List<Category>();
}