namespace PowerUp.Models;

public class Gym
{
    public int Id { get; set; }
    public required ICollection<GroupClass> GroupClasses { get; set; }
    public required ICollection<Instructor> Instructors { get; set; }
    public required ICollection<Member> Members { get; set; }
}