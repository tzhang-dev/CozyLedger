namespace CozyLedger.Domain.Entities;

public class BookInvite : Entity
{
    public Guid BookId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsUsed { get; set; }
    public Guid? UsedByUserId { get; set; }
    public DateTime? UsedAtUtc { get; set; }

    public Book? Book { get; set; }
}