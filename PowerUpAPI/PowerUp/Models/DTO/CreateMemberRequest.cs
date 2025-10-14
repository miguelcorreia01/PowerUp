namespace PowerUp.Models.DTO;
public class CreateMemberRequest
{
    public Guid UserId { get; set; }
    public Guid? InstructorId { get; set; }
}