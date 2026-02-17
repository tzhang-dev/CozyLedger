using System.Security.Claims;

namespace CozyLedger.Api.Extensions;

/// <summary>
/// Provides convenience helpers for extracting identity data from <see cref="ClaimsPrincipal"/>.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Extracts the authenticated user identifier from the current principal.
    /// </summary>
    /// <param name="user">Principal containing identity claims.</param>
    /// <returns>The parsed user identifier.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the user id claim is missing.</exception>
    /// <exception cref="FormatException">Thrown when the user id claim is not a valid GUID.</exception>
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(id ?? throw new InvalidOperationException("Missing user id claim."));
    }
}
