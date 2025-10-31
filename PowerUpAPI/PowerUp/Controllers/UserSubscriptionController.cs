using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerUp.Data;
using PowerUp.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using PowerUp.Models.DTO;

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


 // Subscribe to a subscription plan
    [Authorize(Roles = "User")]
    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest request)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("id")?.Value;
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized("Invalid user.");
        }

        // Check if subscription exists and is not deleted
        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == request.SubscriptionId && !s.IsDeleted);
        
        if (subscription == null)
        {
            return NotFound("Subscription not found.");
        }

        // Check if user already has an active subscription
        var existingSubscription = await _context.UserSubscriptions
            .Include(us => us.Subscription)
            .FirstOrDefaultAsync(us => us.UserId == userId && us.IsActive && !us.IsDeleted);
        
        if (existingSubscription != null)
        {
            return Conflict("You already have an active subscription. Please cancel it first.");
        }

        // Calculate dates
        var startDate = DateTime.UtcNow;
        var endDate = subscription.Type switch
        {
            SubscriptionType.Monthly => startDate.AddMonths(1),
            SubscriptionType.Semestral => startDate.AddMonths(6),
            SubscriptionType.Yearly => startDate.AddYears(1),
            _ => startDate.AddMonths(1)
        };

        var userSubscription = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SubscriptionId = request.SubscriptionId,
            StartDate = startDate,
            EndDate = endDate,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.UserSubscriptions.Add(userSubscription);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Subscribed successfully", userSubscription });
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
