using Assignment1_PRN222_Group7_DAL.Enums;

namespace Assignment1_PRN222_Group7_DAL.Entities
{
    /// <summary>Lịch sử đăng ký gói của người dùng</summary>
    public class UserSubscription
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int PlanId { get; set; }

        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        public DateTime? EndDate { get; set; }

        public bool IsActive { get; set; } = true;

        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        public string? TransactionId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public User User { get; set; } = null!;
        public SubscriptionPlan Plan { get; set; } = null!;
    }
}
