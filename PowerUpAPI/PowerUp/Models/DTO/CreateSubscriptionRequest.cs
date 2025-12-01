namespace PowerUp.Models.DTO;

public class CreateSubscriptionRequest
{
    public string Type { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
}