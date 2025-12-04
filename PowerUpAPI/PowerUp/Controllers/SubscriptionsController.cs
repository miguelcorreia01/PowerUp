using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerUp.Data;
using PowerUp.Models;
using PowerUp.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

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
        return await _context.Subscriptions
            .Where(s => !s.IsDeleted)
            .ToListAsync();
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
    public async Task<ActionResult<Subscription>> CreateSubscription([FromBody] CreateSubscriptionRequest request)
    {
        // Validate enum
        if (!Enum.TryParse<SubscriptionType>(request.Type, ignoreCase: true, out var subscriptionType))
        {
            return BadRequest(new { message = $"Invalid subscription type: {request.Type}. Valid types are: Monthly, Semestral, Yearly" });
        }

        // Validate price
        if (request.TotalPrice <= 0)
        {
            return BadRequest(new { message = "Total price must be greater than 0" });
        }

        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            Type = subscriptionType,
            TotalPrice = request.TotalPrice,
            IsDeleted = false
        };
        
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

  [Authorize(Roles = "Member")]
    [HttpGet("my")]
    public async Task<IActionResult> GetMySubscription()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("id")?.Value;
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized("Invalid user.");
        }
        
        var subscription = await _context.UserSubscriptions
            .Include(us => us.Subscription)
            .Where(us => us.UserId == userId && us.IsActive && !us.IsDeleted)
            .OrderByDescending(us => us.CreatedAt)
            .FirstOrDefaultAsync();

        if (subscription == null) return NotFound();
        return Ok(subscription);
    }


    // Delete subscription
    [Authorize(Roles = "Member")]
    [HttpPost("cancel")]
    public async Task<IActionResult> CancelSubscription()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("id")?.Value;
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized("Invalid user.");
        }

        var userSubscription = await _context.UserSubscriptions
            .FirstOrDefaultAsync(us => us.UserId == userId && us.IsActive && !us.IsDeleted);
        
        if (userSubscription == null)
        {
            return NotFound("No active subscription found.");
        }

        _context.UserSubscriptions.Remove(userSubscription);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Subscription cancelled successfully" });
    }
    private bool SubscriptionExists(Guid id)
    {
        return _context.Subscriptions.Any(e => e.Id == id);
    }   
}