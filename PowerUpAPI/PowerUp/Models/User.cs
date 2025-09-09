namespace PowerUp.Models;

    public class User
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Email { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public required string PasswordHash { get; set; }
        public bool IsAdmin { get; set; } = false;
        public UserRole Role { get; set; } = UserRole.Member;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
    public enum UserRole
    {
        Admin,
        Instructor,
        Member
    }

