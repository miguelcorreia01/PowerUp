using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PowerUp.Data;
using PowerUp.Models;

namespace PowerUp.Controllers;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Roles = "Admin")]
public class AdminDashboardController : ControllerBase
{
    private readonly PowerUpDbContext _context;

    public AdminDashboardController(PowerUpDbContext context)
    {
        _context = context;
    }

    [HttpGet("overview")]
    public async Task<ActionResult<AdminDashboardSummary>> GetOverview()
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        var thirtyDaysAgo = now.AddDays(-30);

        var usersQuery = _context.Users.Where(u => !u.IsDeleted);

        var totalUsers = await usersQuery.CountAsync();
        var totalMembers = await usersQuery.CountAsync(u => u.Role == UserRole.Member);
        var totalInstructors = await usersQuery.CountAsync(u => u.Role == UserRole.Instructor);
        var newUsersLast30Days = await usersQuery.CountAsync(u => u.CreatedAt >= thirtyDaysAgo);

        var activeSubscriptions = await _context.UserSubscriptions
            .CountAsync(us => us.IsActive && !us.IsDeleted);

        var monthlyRevenue = await _context.UserSubscriptions
            .Where(us => us.IsActive && !us.IsDeleted && us.StartDate <= now && us.EndDate >= now)
            .Include(us => us.Subscription)
            .Select(us => new
            {
                Price = us.Subscription!.TotalPrice,
                Type = us.Subscription.Type
            })
            .ToListAsync();

        decimal revenue = 0;
        foreach (var sub in monthlyRevenue)
        {
            int months = sub.Type switch
            {
                SubscriptionType.Monthly => 1,
                SubscriptionType.Semestral => 6,
                SubscriptionType.Yearly => 12,
                _ => 1
            };
            revenue += sub.Price / months;
        }

        return new AdminDashboardSummary
        {
            TotalUsers = totalUsers,
            TotalMembers = totalMembers,
            TotalInstructors = totalInstructors,
            NewUsersLast30Days = newUsersLast30Days,
            ActiveSubscriptions = activeSubscriptions,
            MonthlyRevenue = revenue
        };
    }

    [HttpGet("users-by-month")]
    public async Task<ActionResult<IEnumerable<MonthlyDataPoint>>> GetUsersByMonth()
    {
        var now = DateTime.UtcNow;
        var startDate = now.AddMonths(-11);
        var startOfFirstMonth = new DateTime(startDate.Year, startDate.Month, 1);

        var usersByMonth = await _context.Users
            .Where(u => !u.IsDeleted && u.CreatedAt >= startOfFirstMonth)
            .GroupBy(u => new { u.CreatedAt.Year, u.CreatedAt.Month })
            .Select(g => new MonthlyDataPoint
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Value = g.Count()
            })
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToListAsync();

            
        var result = new List<MonthlyDataPoint>();
        for (int i = 0; i < 12; i++)
        {
            var date = startOfFirstMonth.AddMonths(i);
            var existing = usersByMonth.FirstOrDefault(x => x.Year == date.Year && x.Month == date.Month);
            result.Add(existing ?? new MonthlyDataPoint
            {
                Year = date.Year,
                Month = date.Month,
                Value = 0
            });
        }

        return Ok(result);
    }

    [HttpGet("revenue-by-month")]
    public async Task<ActionResult<IEnumerable<MonthlyDataPoint>>> GetRevenueByMonth()
    {
        var now = DateTime.UtcNow;
        var startDate = now.AddMonths(-11);
        var startOfFirstMonth = new DateTime(startDate.Year, startDate.Month, 1);

        var subscriptionsByMonth = await _context.UserSubscriptions
            .Where(us => !us.IsDeleted && us.StartDate >= startOfFirstMonth)
            .Include(us => us.Subscription)
            .ToListAsync();

        var revenueByMonth = subscriptionsByMonth
            .GroupBy(us => new { us.StartDate.Year, us.StartDate.Month })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Revenue = g.Sum(us =>
                {
                    int months = us.Subscription!.Type switch
                    {
                        SubscriptionType.Monthly => 1,
                        SubscriptionType.Semestral => 6,
                        SubscriptionType.Yearly => 12,
                        _ => 1
                    };
                    return us.Subscription.TotalPrice / months;
                })
            })
            .ToList();

        var result = new List<MonthlyDataPoint>();
        for (int i = 0; i < 12; i++)
        {
            var date = startOfFirstMonth.AddMonths(i);
            var existing = revenueByMonth.FirstOrDefault(x => x.Year == date.Year && x.Month == date.Month);
            result.Add(new MonthlyDataPoint
            {
                Year = date.Year,
                Month = date.Month,
                Value = existing?.Revenue ?? 0
            });
        }

        return Ok(result);
    }
}

public class MonthlyDataPoint
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal Value { get; set; }
}