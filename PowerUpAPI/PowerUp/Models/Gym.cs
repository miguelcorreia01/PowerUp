namespace PowerUp.Models;

public class Gym
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Address { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<GroupClass> GroupClasses { get; set; } = new List<GroupClass>();
    public ICollection<Instructor> Instructors { get; set; } = new List<Instructor>();
    public ICollection<Member> Members { get; set; } = new List<Member>();
}