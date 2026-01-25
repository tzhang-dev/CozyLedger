using CozyLedger.Domain.Enums;

namespace CozyLedger.Domain.Entities;

public class Account : Entity
{
    public Guid BookId { get; set; }
    public string NameEn { get; set; } = string.Empty;
    public string NameZhHans { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public string Currency { get; set; } = "USD";
    public bool IsHidden { get; set; }
    public bool IncludeInNetWorth { get; set; } = true;
    public string? Note { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Book? Book { get; set; }
}