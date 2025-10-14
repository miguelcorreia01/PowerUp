using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerUp.Data;
using PowerUp.Models;
using System.Security.Claims;

namespace PowerUp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly PowerUpDbContext _context;

    public UsersController(PowerUpDbContext context)
    {
        _context = context;
    }

    // Only admins can get all users
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<IEnumerable<User>>> GetUsers()
    {
        return await _context.Users
            .Where(u => !u.IsDeleted)
            .Select(u => new User 
            { 
                Id = u.Id, 
                Name = u.Name, 
                Email = u.Email, 
                PhoneNumber = u.PhoneNumber,
                Role = u.Role,
                IsAdmin = u.IsAdmin,
                CreatedAt = u.CreatedAt,
                UpdatedAt = u.UpdatedAt,
                Password = string.Empty 
            })
            .ToListAsync();
    }

    // Users can get their own profile, admins can get any profile
    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUser(Guid id)
    {
        var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "");
        var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

        // Users can only access their own profile unless they're admin
        if (currentUserId != id && currentUserRole != "Admin")
        {
            return Forbid();
        }

        var user = await _context.Users.FindAsync(id);

        if (user == null || user.IsDeleted)
        {
            return NotFound();
        }

        user.Password = string.Empty;
        return user;
    }

    // Only admins can create users directly
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<User>> CreateUser(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        user.Password = string.Empty;
        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }

    // Users can update their own profile, admins can update any profile
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, User user)
    {
        var currentUserId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "");
        var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;


        if (currentUserId != id && currentUserRole != "Admin")
        {
            return Forbid();
        }

        if (id != user.Id)
        {
            return BadRequest();
        }

        _context.Entry(user).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!UserExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }


    [Authorize(Roles = "Admin")]
    [HttpPost("promote/{userId}")]
    public async Task<IActionResult> PromoteToInstructor(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        user.Role = UserRole.Instructor;
        await _context.SaveChangesAsync();
        return Ok(new { message = "User promoted to Instructor." });
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("distribution")]
    public IActionResult GetUserDistribution()
    {
        var distribution = _context.Users
            .GroupBy(u => u.Role)
            .Select(g => new { Role = g.Key, Count = g.Count() })
            .ToList();

        return Ok(distribution);
    }


    //delete users (admins only, soft delete)
    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private bool UserExists(Guid id)
    {
        return _context.Users.Any(e => e.Id == id);
    }
}