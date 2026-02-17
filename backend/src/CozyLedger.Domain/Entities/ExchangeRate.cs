namespace CozyLedger.Domain.Entities;

/// <summary>
/// Represents an exchange rate used for cross-currency reporting.
/// </summary>
public class ExchangeRate : Entity
{
    /// <summary>
    /// Gets or sets the base currency code of the rate.
    /// </summary>
    public string BaseCurrency { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the quote currency code of the rate.
    /// </summary>
    public string QuoteCurrency { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the conversion factor from base to quote currency.
    /// </summary>
    public decimal Rate { get; set; }

    /// <summary>
    /// Gets or sets the UTC date when the rate becomes effective.
    /// </summary>
    public DateTime EffectiveDateUtc { get; set; }

    /// <summary>
    /// Gets or sets the source label for this rate.
    /// </summary>
    public string Source { get; set; } = string.Empty;
}
