namespace PowerUp.Models.DTO;

public class UpdateMemberRequest
{
    public Guid? InstructorId { get; set; }
    public bool IsActive { get; set; }
}