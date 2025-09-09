namespace PowerUp.Models;

    public class Instructor : User
    {
        public ICollection<Member> Members { get; set; } = new List<Member>();
    }
