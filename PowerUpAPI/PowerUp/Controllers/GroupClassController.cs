using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerUp.Data;
using PowerUp.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;


namespace PowerUp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GroupClassController : ControllerBase
{
    private readonly PowerUpDbContext _context;

    public GroupClassController(PowerUpDbContext context)
    {
        _context = context;
    }


    // GET all group classes
    [HttpGet]
    public async Task<ActionResult<IEnumerable<GroupClass>>> GetGroupClasses()
    {
        return await _context.GroupClasses
            .Include(gc => gc.Members)
            .Include(gc => gc.Instructor).ThenInclude(i => i.User)
            .Where(gc => !gc.IsDeleted)
            .ToListAsync();
    }


    // get group class by id
    [HttpGet("{id}")]
    public async Task<ActionResult<GroupClass>> GetGroupClass(Guid id)
    {
        var groupClass = await _context.GroupClasses
            .Include(gc => gc.Members)
            .Include(gc => gc.Instructor).ThenInclude(i => i.User)
            .FirstOrDefaultAsync(gc => gc.Id == id && !gc.IsDeleted);

        if (groupClass == null) return NotFound();
        return groupClass;
    }

    // Create group class
    [HttpPost]
    public async Task<ActionResult<GroupClass>> CreateGroupClass(GroupClass groupClass)
    {
        _context.GroupClasses.Add(groupClass);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetGroupClass), new { id = groupClass.Id }, groupClass);
    }

    // Update group class
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateGroupClass(Guid id, GroupClass groupClass)
    {
        if (id != groupClass.Id)
        {
            return BadRequest();
        }

        _context.Entry(groupClass).State = EntityState.Modified;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!GroupClassExists(id))
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

    // Delete group class
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteGroupClass(Guid id)
    {
        var groupClass = await _context.GroupClasses.FindAsync(id);
        if (groupClass == null)
        {
            return NotFound();
        }

        groupClass.IsDeleted = true;
        groupClass.DeletedAt = DateTime.UtcNow;
        // _context.GroupClasses.Remove(groupClass);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // Enroll in group class
    [Authorize(Roles = "User")]
    [HttpPost("{id:guid}/enroll")]
    public async Task<IActionResult> Enroll(Guid id)
    {
        //current user id from JWT
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("id")?.Value;
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized("Invalid user.");
        }

        // Load member by UserId
        var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == userId);
        if (member == null)
        {
            return NotFound("Member profile not found.");
        }

        // Load class with members
        var groupClass = await _context.GroupClasses
            .Include(gc => gc.Members)
            .FirstOrDefaultAsync(gc => gc.Id == id && !gc.IsDeleted);

        if (groupClass == null)
        {
            return NotFound("Group class not found.");
        }

        if (groupClass.Members.Any(m => m.Id == member.Id))
        {
            return Conflict("Already enrolled in this class.");
        }

        if (groupClass.CurrentEnrollment >= groupClass.MaxCapacity)
        {
            return Conflict("Class is full.");
        }

        groupClass.Members.Add(member);
        groupClass.CurrentEnrollment += 1;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Enrolled successfully" });
    }

    // Unenroll from group class
    [Authorize(Roles = "User")]
    [HttpPost("{id:guid}/unenroll")]
    public async Task<IActionResult> Unenroll(Guid id)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("id")?.Value;
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized("Invalid user.");
        }

        var member = await _context.Members.FirstOrDefaultAsync(m => m.UserId == userId);
        if (member == null)
        {
            return NotFound("Member profile not found.");
        }

        var groupClass = await _context.GroupClasses
            .Include(gc => gc.Members)
            .FirstOrDefaultAsync(gc => gc.Id == id && !gc.IsDeleted);

        if (groupClass == null)
        {
            return NotFound("Group class not found.");
        }

        var existing = groupClass.Members.FirstOrDefault(m => m.Id == member.Id);
        if (existing == null)
        {
            return NotFound("You are not enrolled in this class.");
        }

        groupClass.Members.Remove(existing);
        groupClass.CurrentEnrollment = Math.Max(0, groupClass.CurrentEnrollment - 1);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Unenrolled successfully" });
    }
    


    private bool GroupClassExists(Guid id)
    {
        return _context.GroupClasses.Any(e => e.Id == id);
    }
}