namespace CozyLedger.Domain.Enums;

/// <summary>
/// Defines supported transaction types.
/// </summary>
public enum TransactionType
{
    /// <summary>
    /// Expense transaction.
    /// </summary>
    Expense = 0,

    /// <summary>
    /// Income transaction.
    /// </summary>
    Income = 1,

    /// <summary>
    /// Account-to-account transfer transaction.
    /// </summary>
    Transfer = 2,

    /// <summary>
    /// Balance adjustment transaction.
    /// </summary>
    BalanceAdjustment = 3,

    /// <summary>
    /// Liability adjustment transaction.
    /// </summary>
    LiabilityAdjustment = 4
}
