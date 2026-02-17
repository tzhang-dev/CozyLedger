using Microsoft.AspNetCore.Identity;

namespace CozyLedger.Infrastructure.Identity;

/// <summary>
/// Represents an authenticated CozyLedger user account.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>
    /// Gets or sets the user's display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the preferred locale code for the user.
    /// </summary>
    public string Locale { get; set; } = "en";
}
