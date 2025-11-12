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

        var monthlyRevenue = await _context.Payments
            .Where(p => p.PaymentDate >= startOfMonth &&
                        p.Status == PaymentStatus.Completed &&
                        !p.IsDeleted)
            .SumAsync(p => p.Amount);

        var topMembership = await _context.UserSubscriptions
            .Where(us => !us.IsDeleted)
            .GroupBy(us => us.SubscriptionId)
            .Select(g => new
            {
                SubscriptionId = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(g => g.Count)
            .FirstOrDefaultAsync();

        string? topMembershipName = null;
        int topMembershipCount = 0;

        if (topMembership != null)
        {
            var topSub = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.Id == topMembership.SubscriptionId);
            if (topSub != null)
            {
                topMembershipName = $"{topSub.Type} ({topSub.TotalPrice:C0})";
                topMembershipCount = topMembership.Count;
            }
        }

        var upcomingGroupClasses = await _context.GroupClasses
            .CountAsync(gc => !gc.IsDeleted && gc.StartTime >= now);

        var ptSessionsNext7Days = await _context.PtSessions
            .CountAsync(pt => !pt.IsDeleted &&
                              pt.SessionTime >= now &&
                              pt.SessionTime <= now.AddDays(7));

        return new AdminDashboardSummary
        {
            TotalUsers = totalUsers,
            TotalMembers = totalMembers,
            TotalInstructors = totalInstructors,
            NewUsersLast30Days = newUsersLast30Days,
            ActiveSubscriptions = activeSubscriptions,
            MonthlyRevenue = monthlyRevenue,
            TopMembership = topMembershipName,
            TopMembershipSubscriptions = topMembershipCount,
            UpcomingGroupClasses = upcomingGroupClasses,
            PersonalTrainingSessionsNext7Days = ptSessionsNext7Days
        };
    }
}