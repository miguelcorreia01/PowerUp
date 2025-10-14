using PowerUp.Models;

namespace PowerUp.Services;

public interface IJwtService
{
    string GenerateToken(User user);
    Guid? GetUserIdFromToken(string token);
    string? GetUserRoleFromToken(string token);
}