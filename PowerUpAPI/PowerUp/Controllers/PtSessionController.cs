using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerUp.Data;
using PowerUp.Models;
using PowerUp.Models.DTO;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;



namespace PowerUp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PtSessionController : ControllerBase
{
    private readonly PowerUpDbContext _context;

    public PtSessionController(PowerUpDbContext context)
    {
        _context = context;
    }

    // GET all PT sessions
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PtSession>>> GetPtSessions()
    {
        return await _context.PtSessions.ToListAsync();
    }

    // GET PT session by id
    [HttpGet("{id}")]
    public async Task<ActionResult<PtSession>> GetPtSession(Guid id)
    {
        var ptSession = await _context.PtSessions.FindAsync(id);

        if (ptSession == null)
        {
            return NotFound();
        }

        return ptSession;
    }

    // Create PT session
    [HttpPost]
    public async Task<ActionResult<PtSession>> CreatePtSession(PtSession ptSession)
    {
        _context.PtSessions.Add(ptSession);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPtSession), new { id = ptSession.Id }, ptSession);
    }

    // Update PT session
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePtSession(Guid id, PtSession ptSession)
    {
        if (id != ptSession.Id)
        {
            return BadRequest();
        }

        _context.Entry(ptSession).State = EntityState.Modified;
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!PtSessionExists(id))
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

    [Authorize(Roles = "User")]
    [HttpPost("book")]
    public async Task<IActionResult> BookSession([FromBody] BookSessionRequest request)
    {
        var memberId = Guid.Parse(User.FindFirst("id").Value);
        var session = new PtSession
        {
            MemberId = memberId,
            InstructorId = request.InstructorId,
            SessionTime = request.SessionTime,
        };
        _context.PtSessions.Add(session);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Session booked successfully" });
    }


    //Soft DELETE PT session
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePtSession(Guid id)
    {
        var ptSession = await _context.PtSessions.FindAsync(id);
        if (ptSession == null)
        {
            return NotFound();
        }

        //_context.PtSessions.Remove(ptSession);
        ptSession.IsDeleted = true;
        ptSession.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool PtSessionExists(Guid id)
    {
        return _context.PtSessions.Any(e => e.Id == id);
    }
}