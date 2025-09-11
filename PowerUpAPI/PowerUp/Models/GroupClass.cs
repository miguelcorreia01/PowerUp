namespace PowerUp.Models;

public class GroupClass
{
    public int Id { get; set; }
    public Guid InstructorId { get; set; }
    public Instructor? Instructor { get; set; }
    public GroupClassType Type { get; set; }
    public ICollection<Member> Members { get; set; } = new List<Member>();
    public DateTime StartTime { get; set; }
    public int MaxCapacity { get; set; }
    public int CurrentEnrollment { get; set; } = 0;
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
}

public enum GroupClassType
{
    Yoga,
    Pilates,
    Spinning,
    Zumba,
    Crossfit,
    HIIT,
    StrengthTraining,
    Cardio,
    Jumping,
    ABS
}