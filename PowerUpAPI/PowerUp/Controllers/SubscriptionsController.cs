using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerUp.Data;
using PowerUp.Models;
using Microsoft.AspNetCore.Authorization;

namespace PowerUp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubscriptionsController : ControllerBase
{
    private readonly PowerUpDbContext _context;

    public SubscriptionsController(PowerUpDbContext context)
    {
        _context = context;
    }

    // GET all subscriptions
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Subscription>>> GetSubscriptions()
    {
        return await _context.Subscriptions.ToListAsync();
    }

    // GET subscription by id
    [HttpGet("{id}")]
    public async Task<ActionResult<Subscription>> GetSubscription(Guid id)
    {
        var subscription = await _context.Subscriptions.FindAsync(id);

        if (subscription == null)
        {
            return NotFound();
        }

        return subscription;
    }

    // Create subscription
    [HttpPost]
    public async Task<ActionResult<Subscription>> CreateSubscription(Subscription subscription)
    {
        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSubscription), new { id = subscription.Id }, subscription);
    }

    // Update subscription
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSubscription(Guid id, Subscription subscription)
    {
        if (id != subscription.Id)
        {
            return BadRequest();
        }

        _context.Entry(subscription).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!SubscriptionExists(id))
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
    [HttpGet("my")]
    public async Task<IActionResult> GetMySubscription()
    {
        var userId = Guid.Parse(User.FindFirst("id").Value);
        var subscription = await _context.UserSubscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (subscription == null) return NotFound();
        return Ok(subscription);
    }


    // Delete subscription
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSubscription(Guid id)
    {
        var subscription = await _context.Subscriptions.FindAsync(id);
        if (subscription == null)
        {
            return NotFound();
        }

        // _context.Subscriptions.Remove(subscription);
        subscription.IsDeleted = true;
        subscription.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool SubscriptionExists(Guid id)
    {
        return _context.Subscriptions.Any(e => e.Id == id);
    }   
}