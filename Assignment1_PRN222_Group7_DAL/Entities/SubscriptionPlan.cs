using Assignment1_PRN222_Group7_DAL.Enums;

namespace Assignment1_PRN222_Group7_DAL.Entities
{
    /// <summary>Gói đăng ký dịch vụ: Free, Basic, Premium</summary>
    public class SubscriptionPlan
    {
        public int Id { get; set; }

        public SubscriptionTier Tier { get; set; }

        public string Name { get; set; } = string.Empty; // "Free", "Basic", "Premium"

        public string? Description { get; set; }

        /// <summary>Số tài liệu tối đa được upload (-1 = unlimited)</summary>
        public int MaxDocumentsUpload { get; set; }

        /// <summary>Số chat sessions tối đa mỗi ngày (-1 = unlimited)</summary>
        public int MaxChatsPerDay { get; set; }

        /// <summary>Số tin nhắn tối đa mỗi session (-1 = unlimited)</summary>
        public int MaxMessagesPerSession { get; set; }

        /// <summary>Số môn học tối đa có thể truy cập (-1 = unlimited)</summary>
        public int MaxSubjectsAccess { get; set; }

        public decimal Price { get; set; } // VND per month

        public bool IsActive { get; set; } = true;

        // Navigation
        public ICollection<UserSubscription> UserSubscriptions { get; set; } = [];
    }
}
