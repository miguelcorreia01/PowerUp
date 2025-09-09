namespace PowerUp.Models;

    public class PtSession
    {
        public int Id { get; set; }
        public Guid InstructorId { get; set; }
        public Instructor? Instructor { get; set; }
        public Guid MemberId { get; set; }
        public Member? Member { get; set; }
        public decimal Price { get; set; }
        public DateTime SessionTime { get; set; }
        public string Notes { get; set; } = string.Empty;
        public SessionStatus Status { get; set; } = SessionStatus.Scheduled;
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

    }
    public enum SessionStatus
{
    Scheduled,
    Completed,
    Cancelled,
    NoShow
}

