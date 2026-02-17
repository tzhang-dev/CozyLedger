namespace CozyLedger.Domain.Enums;

/// <summary>
/// Defines supported account classifications.
/// </summary>
public enum AccountType
{
    /// <summary>
    /// Physical cash account.
    /// </summary>
    Cash = 0,

    /// <summary>
    /// Bank account.
    /// </summary>
    Bank = 1,

    /// <summary>
    /// Credit card account.
    /// </summary>
    CreditCard = 2,

    /// <summary>
    /// Investment account.
    /// </summary>
    Investment = 3,

    /// <summary>
    /// Asset account.
    /// </summary>
    Asset = 4,

    /// <summary>
    /// Liability account.
    /// </summary>
    Liability = 5
}
