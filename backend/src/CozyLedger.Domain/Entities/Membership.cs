namespace CozyLedger.Domain.Entities;

public class Membership : Entity
{
    public Guid BookId { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Book? Book { get; set; }
}