using Microsoft.EntityFrameworkCore;
using PowerUp.Models;

namespace PowerUp.Data;

public class PowerUpDbContext : DbContext
{
    public PowerUpDbContext(DbContextOptions<PowerUpDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<Instructor> Instructors => Set<Instructor>();
    public DbSet<Gym> Gyms => Set<Gym>();
    public DbSet<GroupClass> GroupClasses => Set<GroupClass>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<UserSubscription> UserSubscriptions => Set<UserSubscription>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PtSession> PtSessions => Set<PtSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // Configure Member-Instructor relationship
        modelBuilder.Entity<Member>()
            .HasOne(m => m.Instructor)
            .WithMany(i => i.Members)
            .HasForeignKey(m => m.InstructorId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure GroupClass-Instructor relationship
        modelBuilder.Entity<GroupClass>()
            .HasOne(gc => gc.Instructor)
            .WithMany()
            .HasForeignKey(gc => gc.InstructorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure PtSession relationships
        modelBuilder.Entity<PtSession>()
            .HasOne(ps => ps.Instructor)
            .WithMany()
            .HasForeignKey(ps => ps.InstructorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PtSession>()
            .HasOne(ps => ps.Member)
            .WithMany()
            .HasForeignKey(ps => ps.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure UserSubscription relationships
        modelBuilder.Entity<UserSubscription>()
            .HasOne(us => us.User)
            .WithMany(u => u.UserSubscriptions)
            .HasForeignKey(us => us.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserSubscription>()
            .HasOne(us => us.Subscription)
            .WithMany()
            .HasForeignKey(us => us.SubscriptionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure Payment relationship
        modelBuilder.Entity<Payment>()
            .HasOne(p => p.UserSubscription)
            .WithMany(us => us.Payments)
            .HasForeignKey(p => p.UserSubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure many-to-many for GroupClass and Members
        modelBuilder.Entity<GroupClass>()
            .HasMany(gc => gc.Members)
            .WithMany()
            .UsingEntity(j => j.ToTable("GroupClassMembers"));

        // Configure unique constraint for UserSubscription
        modelBuilder.Entity<UserSubscription>()
            .HasIndex(us => new { us.UserId, us.SubscriptionId })
            .IsUnique();
    }
}