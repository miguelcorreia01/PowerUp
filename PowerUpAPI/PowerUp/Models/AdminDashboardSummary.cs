public class AdminDashboardSummary
{
    public int TotalUsers { get; set; }
    public int TotalMembers { get; set; }
    public int TotalInstructors { get; set; }
    public int NewUsersLast30Days { get; set; }
    public int ActiveSubscriptions { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public string? TopMembership { get; set; }
    public int TopMembershipSubscriptions { get; set; }
    public int UpcomingGroupClasses { get; set; }
    public int PersonalTrainingSessionsNext7Days { get; set; }
}