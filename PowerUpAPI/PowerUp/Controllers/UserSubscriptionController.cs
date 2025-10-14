using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerUp.Data;
using PowerUp.Models;

namespace PowerUp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserSubscriptionController : ControllerBase
{
    private readonly PowerUpDbContext _context;

    public UserSubscriptionController(PowerUpDbContext context)
    {
        _context = context;
    }

    // GET all user subscriptions
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserSubscription>>> GetUserSubscriptions()
    {
        return await _context.UserSubscriptions.ToListAsync();
    }

    // GET user subscription by id
    [HttpGet("{id}")]
    public async Task<ActionResult<UserSubscription>> GetUserSubscription(Guid id)
    {
        var userSubscription = await _context.UserSubscriptions.FindAsync(id);

        if (userSubscription == null)
        {
            return NotFound();
        }

        return userSubscription;
    }

    // Create user subscription
    [HttpPost]
    public async Task<ActionResult<UserSubscription>> CreateUserSubscription(UserSubscription userSubscription)
    {
        _context.UserSubscriptions.Add(userSubscription);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUserSubscription), new { id = userSubscription.Id }, userSubscription);
    }

    // Update user subscription
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUserSubscription(Guid id, UserSubscription userSubscription)
    {
        if (id != userSubscription.Id)
        {
            return BadRequest();
        }

        _context.Entry(userSubscription).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!UserSubscriptionExists(id))
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

    // Delete user subscription
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUserSubscription(Guid id)
    {
        var userSubscription = await _context.UserSubscriptions.FindAsync(id);
        if (userSubscription == null)
        {
            return NotFound();
        }

        //_context.UserSubscriptions.Remove(userSubscription);
        userSubscription.IsDeleted = true;
        userSubscription.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool UserSubscriptionExists(Guid id)
    {
        return _context.UserSubscriptions.Any(e => e.Id == id);
    }
}
