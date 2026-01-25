namespace CozyLedger.Domain.Entities;

public class Book : Entity
{
    public string Name { get; set; } = string.Empty;
    public string BaseCurrency { get; set; } = "USD";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<Membership> Memberships { get; set; } = new List<Membership>();
}