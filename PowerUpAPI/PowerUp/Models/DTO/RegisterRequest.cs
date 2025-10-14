using System.ComponentModel.DataAnnotations;

namespace PowerUp.Models.DTO;

public class RegisterRequest
{

    [Required]
    public required string Name { get; set; }

    
    [Required, EmailAddress]
    public required string Email { get; set; }

    [Required, Phone]
    public string? PhoneNumber { get; set; }

    [Required, MinLength(6)]
    public required string Password { get; set; }
}