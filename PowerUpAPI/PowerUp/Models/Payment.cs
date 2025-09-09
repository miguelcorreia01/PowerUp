
namespace PowerUp.Models;

public class Payment
{
    public int Id { get; set; }
    public int UserSubscriptionId { get; set; }
    public required UserSubscription UserSubscription { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public PaymentStatus Status { get; set; }
    public string? TransactionId { get; set; }

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }

}

public enum PaymentStatus
{
    Pending, Completed, Failed, Refunded
}