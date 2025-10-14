using Microsoft.IdentityModel.Tokens;
using PowerUp.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PowerUp.Services;

public class JwtService : IJwtService
{
    public string GenerateToken(User user)
    {
        var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") 
                     ?? throw new InvalidOperationException("JWT_KEY not found in environment variables.");

        var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") 
                        ?? throw new InvalidOperationException("JWT_ISSUER not found in environment variables.");

        var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") 
                          ?? throw new InvalidOperationException("JWT_AUDIENCE not found in environment variables.");

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("IsAdmin", user.IsAdmin.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public Guid? GetUserIdFromToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jsonToken = tokenHandler.ReadJwtToken(token);
            var userIdClaim = jsonToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);

            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }
        }
        catch
        {
            // Token is invalid
        }

        return null;
    }

    public string? GetUserRoleFromToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jsonToken = tokenHandler.ReadJwtToken(token);
            var roleClaim = jsonToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Role);

            return roleClaim?.Value;
        }
        catch
        {
            // Token is invalid
        }

        return null;
    }
}
