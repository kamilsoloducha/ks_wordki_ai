using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Wordki.Modules.Users.Application.Abstractions;

namespace Wordki.Modules.Users.Infrastructure.Security;

public sealed class JwtBearerTokenService(
    IOptions<BearerTokenOptions> options,
    TimeProvider timeProvider) : IBearerTokenService
{
    private readonly BearerTokenOptions _options = options.Value;

    public string CreateToken(BearerTokenPayload payload)
    {
        var key = GetSigningKey(_options.SecretKey);
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, payload.UserId.ToString()),
            new(JwtRegisteredClaimNames.Email, payload.Email),
            new(ClaimTypes.Role, payload.Role)
        };

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var expires = now.AddMinutes(_options.ExpirationMinutes);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public BearerTokenValidationResult ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return new BearerTokenValidationResult(false, Error: "Token is empty.");
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _options.Issuer,
            ValidateAudience = true,
            ValidAudience = _options.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = GetSigningKey(_options.SecretKey),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            var userIdClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            var emailClaim = principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
            var roleClaim = principal.FindFirst(ClaimTypes.Role)?.Value;

            var userIdParsed = Guid.TryParse(userIdClaim, out var userId);
            if (!userIdParsed || string.IsNullOrWhiteSpace(emailClaim) || string.IsNullOrWhiteSpace(roleClaim))
            {
                return new BearerTokenValidationResult(false, Error: "Token claims are invalid.");
            }

            return new BearerTokenValidationResult(true, userId, emailClaim, roleClaim);
        }
        catch (Exception ex)
        {
            return new BearerTokenValidationResult(false, Error: ex.Message);
        }
    }

    private static SymmetricSecurityKey GetSigningKey(string secret)
    {
        var secretBytes = Encoding.UTF8.GetBytes(secret);
        return new SymmetricSecurityKey(secretBytes);
    }
}
