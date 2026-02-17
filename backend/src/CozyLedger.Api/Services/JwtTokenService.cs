using CozyLedger.Api.Options;
using CozyLedger.Infrastructure.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CozyLedger.Api.Services;

/// <summary>
/// Creates JWT bearer tokens for authenticated users.
/// </summary>
public class JwtTokenService
{
    private readonly JwtOptions _options;
    private readonly SigningCredentials _credentials;

    /// <summary>
    /// Initializes a new instance of the <see cref="JwtTokenService"/> class.
    /// </summary>
    /// <param name="options">JWT configuration values.</param>
    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
        var keyBytes = Encoding.UTF8.GetBytes(_options.Key);
        var securityKey = new SymmetricSecurityKey(keyBytes);
        _credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
    }

    /// <summary>
    /// Creates a signed JWT token for the specified user.
    /// </summary>
    /// <param name="user">Authenticated user for which the token is generated.</param>
    /// <returns>Token payload and expiration metadata.</returns>
    public TokenResult CreateToken(ApplicationUser user)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_options.ExpiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, user.DisplayName)
        };

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: _credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return new TokenResult(tokenString, expires);
    }
}

/// <summary>
/// Represents a generated JWT token and its expiration timestamp.
/// </summary>
/// <param name="Token">Serialized bearer token string.</param>
/// <param name="ExpiresAtUtc">Token expiration in UTC.</param>
public record TokenResult(string Token, DateTime ExpiresAtUtc);
