namespace PowerUp.Models;

    public class Subscription
    {
        public Guid Id { get; set; }
        public SubscriptionType Type { get; set; }
        public decimal TotalPrice { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

    }
    public enum SubscriptionType
    {
        Monthly,
        Semestral,
        Yearly
    }
