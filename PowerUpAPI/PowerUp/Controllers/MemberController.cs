using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerUp.Data;
using PowerUp.Models;
using PowerUp.Models.DTO;
using System.Security.Claims;

namespace PowerUp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MemberController : ControllerBase
{
    private readonly PowerUpDbContext _context;

    public MemberController(PowerUpDbContext context)
    {
        _context = context;
    }

    // GET all members with their user data
    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<IEnumerable<object>>> GetMembers()
    {
        return await _context.Members
            .Include(m => m.User)
            .Include(m => m.Instructor)
                .ThenInclude(i => i!.User)
            .Where(m => !m.User!.IsDeleted)
            .Select(m => new
            {
                m.Id,
                m.UserId,
                User = new
                {
                    m.User!.Id,
                    m.User.Name,
                    m.User.Email,
                    m.User.PhoneNumber,
                    m.User.Role,
                    m.User.IsAdmin,
                    m.User.CreatedAt,
                    m.User.UpdatedAt
                },
                m.InstructorId,
                Instructor = m.Instructor != null ? new
                {
                    m.Instructor.Id,
                    m.Instructor.UserId,
                    User = new
                    {
                        m.Instructor.User!.Name,
                        m.Instructor.User.Email
                    }
                } : null,
                m.IsActive,
                m.CreatedAt,
                m.UpdatedAt
            })
            .ToListAsync();
    }

    // GET member by id with user data
    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetMember(Guid id)
    {
        var member = await _context.Members
            .Include(m => m.User)
            .Include(m => m.Instructor)
                .ThenInclude(i => i!.User)
            .FirstOrDefaultAsync(m => m.Id == id && !m.User!.IsDeleted);

        if (member == null)
        {
            return NotFound();
        }

        return new
        {
            member.Id,
            member.UserId,
            User = new
            {
                member.User!.Id,
                member.User.Name,
                member.User.Email,
                member.User.PhoneNumber,
                member.User.Role,
                member.User.IsAdmin,
                member.User.CreatedAt,
                member.User.UpdatedAt
            },
            member.InstructorId,
            Instructor = member.Instructor != null ? new
            {
                member.Instructor.Id,
                member.Instructor.UserId,
                User = new
                {
                    member.Instructor.User!.Name,
                    member.Instructor.User.Email
                }
            } : null,
            member.IsActive,
            member.CreatedAt,
            member.UpdatedAt
        };
    }

    // Create member from existing user
    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<Member>> CreateMember([FromBody] CreateMemberRequest request)
    {
        // Verify user exists and is not already a member
        var user = await _context.Users.FindAsync(request.UserId);
        if (user == null || user.IsDeleted)
        {
            return NotFound("User not found");
        }

        var existingMember = await _context.Members
            .FirstOrDefaultAsync(m => m.UserId == request.UserId);
        if (existingMember != null)
        {
            return Conflict("User is already a member");
        }

        // Verify instructor exists if provided
        if (request.InstructorId.HasValue)
        {
            var instructor = await _context.Instructors
                .FirstOrDefaultAsync(i => i.Id == request.InstructorId.Value);
            if (instructor == null)
            {
                return BadRequest("Instructor not found");
            }
        }

        var member = new Member
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            InstructorId = request.InstructorId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Members.Add(member);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMember), new { id = member.Id }, member);
    }

    // Update member
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateMember(Guid id, [FromBody] UpdateMemberRequest request)
    {
        var member = await _context.Members.FindAsync(id);
        if (member == null)
        {
            return NotFound();
        }

        // Verify instructor exists if provided
        if (request.InstructorId.HasValue)
        {
            var instructor = await _context.Instructors
                .FirstOrDefaultAsync(i => i.Id == request.InstructorId.Value);
            if (instructor == null)
            {
                return BadRequest("Instructor not found");
            }
        }

        member.InstructorId = request.InstructorId;
        member.IsActive = request.IsActive;
        member.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!MemberExists(id))
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

    // Delete member (soft delete via User)
    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> DeleteMember(Guid id)
    {
        var member = await _context.Members
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (member == null)
        {
            return NotFound();
        }

        // Soft delete the user (which will affect the member)
        member.User!.IsDeleted = true;
        member.User.DeletedAt = DateTime.UtcNow;
        member.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    private bool MemberExists(Guid id)
    {
        return _context.Members.Any(e => e.Id == id);
    }
}