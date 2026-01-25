namespace CozyLedger.Domain.Entities;

public class ExchangeRate : Entity
{
    public string BaseCurrency { get; set; } = string.Empty;
    public string QuoteCurrency { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public DateTime EffectiveDateUtc { get; set; }
    public string Source { get; set; } = string.Empty;
}