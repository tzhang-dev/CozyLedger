namespace CozyLedger.Domain.Entities;

public class Tag : Entity
{
    public Guid BookId { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string NameZhHans { get; set; } = string.Empty;

    public Book? Book { get; set; }
    public ICollection<TransactionTag> TransactionTags { get; set; } = new List<TransactionTag>();
}