using System.Security.Claims;

namespace CozyLedger.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(id ?? throw new InvalidOperationException("Missing user id claim."));
    }
}