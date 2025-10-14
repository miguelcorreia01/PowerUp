using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerUp.Data;
using PowerUp.Models;
using PowerUp.Models.DTO;

namespace PowerUp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InstructorController : ControllerBase
{
    private readonly PowerUpDbContext _context;

    public InstructorController(PowerUpDbContext context)
    {
        _context = context;
    }

    // GET all instructors with their user data
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<IEnumerable<object>>> GetInstructors()
    {
        return await _context.Instructors
            .Include(i => i.User)
            .Where(i => !i.User!.IsDeleted)
            .Select(i => new
            {
                i.Id,
                i.UserId,
                User = new
                {
                    i.User!.Id,
                    i.User.Name,
                    i.User.Email,
                    i.User.PhoneNumber,
                    i.User.Role,
                    i.User.IsAdmin,
                    i.User.CreatedAt,
                    i.User.UpdatedAt
                },
            })
            .ToListAsync();
    }

    // GET instructor by id with user data
    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetInstructor(Guid id)
    {
        var instructor = await _context.Instructors
            .Include(i => i.User)
            .FirstOrDefaultAsync(i => i.Id == id && !i.User!.IsDeleted);

        if (instructor == null)
        {
            return NotFound();
        }

        return new
        {
            instructor.Id,
            instructor.UserId,
            User = new
            {
                instructor.User!.Id,
                instructor.User.Name,
                instructor.User.Email,
                instructor.User.PhoneNumber,
                instructor.User.Role,
                instructor.User.IsAdmin,
                instructor.User.CreatedAt,
                instructor.User.UpdatedAt
            },
            instructor.CreatedAt,
            instructor.UpdatedAt
        };
    }

    // Create instructor from existing user
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<Instructor>> CreateInstructor([FromBody] CreateInstructorRequest request)
    {
        // Verify user exists and is not already an instructor
        var user = await _context.Users.FindAsync(request.UserId);
        if (user == null || user.IsDeleted)
        {
            return NotFound("User not found");
        }

        var existingInstructor = await _context.Instructors
            .FirstOrDefaultAsync(i => i.UserId == request.UserId);
        if (existingInstructor != null)
        {
            return Conflict("User is already an instructor");
        }

        var instructor = new Instructor
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Instructors.Add(instructor);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetInstructor), new { id = instructor.Id }, instructor);
    }

    // Update instructor
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateInstructor(Guid id)
    {
        var instructor = await _context.Instructors.FindAsync(id);
        if (instructor == null)
        {
            return NotFound();
        }

        instructor.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!InstructorExists(id))
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

    // Delete instructor (soft delete via User)
    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteInstructor(Guid id)
    {
        var instructor = await _context.Instructors
            .Include(i => i.User)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (instructor == null)
        {
            return NotFound();
        }

        // Soft delete the user (which will affect the instructor)
        instructor.User!.IsDeleted = true;
        instructor.User.DeletedAt = DateTime.UtcNow;
        instructor.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    private bool InstructorExists(Guid id)
    {
        return _context.Instructors.Any(e => e.Id == id);
    }
}