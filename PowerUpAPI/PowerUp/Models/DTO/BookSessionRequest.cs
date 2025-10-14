namespace PowerUp.Models.DTO;

public class BookSessionRequest
{
    public Guid InstructorId { get; set; }
    public DateTime SessionTime { get; set; }
}