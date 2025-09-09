namespace PowerUp.Models;

    public class UserSubscription

    {
        public required int Id { get; set; }
        public Guid UserId { get; set; }
        public User? User { get; set; }
        public int SubscriptionId { get; set; }
        public Subscription? Subscription { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; } = true;

         public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

    }
