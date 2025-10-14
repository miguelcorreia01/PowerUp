using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerUp.Data;
using PowerUp.Models;

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
        return await _context.GroupClasses.ToListAsync();
    }

    // GET group class by id
    [HttpGet("{id}")]
    public async Task<ActionResult<GroupClass>> GetGroupClass(Guid id)
    {
        var groupClass = await _context.GroupClasses.FindAsync(id);

        if (groupClass == null)
        {
            return NotFound();
        }

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

    private bool GroupClassExists(Guid id)
    {
        return _context.GroupClasses.Any(e => e.Id == id);
    }
}