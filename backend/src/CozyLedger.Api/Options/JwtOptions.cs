namespace CozyLedger.Api.Options;

/// <summary>
/// Represents JWT configuration values used for token issuance and validation.
/// </summary>
public class JwtOptions
{
    /// <summary>
    /// Gets the configuration section name for these options.
    /// </summary>
    public const string SectionName = "Jwt";

    /// <summary>
    /// Gets or sets the expected token issuer.
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the expected token audience.
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the symmetric signing key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets token lifetime in minutes.
    /// </summary>
    public int ExpiryMinutes { get; set; } = 60;
}
