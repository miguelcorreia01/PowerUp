namespace PowerUp.Models;

    public class Member : User
    {
        public Guid InstructorId { get; set; }
        public Instructor? Instructor { get; set; }

        public bool IsActive { get; set; } = true;


    }
