using Microsoft.AspNetCore.Identity;

namespace CozyLedger.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
    public string Locale { get; set; } = "en";
}