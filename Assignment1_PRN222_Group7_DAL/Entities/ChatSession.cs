namespace Assignment1_PRN222_Group7_DAL.Entities
{
    /// <summary>Phiên chat hội thoại của người dùng</summary>
    public class ChatSession
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        /// <summary>Giới hạn trong phạm vi môn học nào (null = không giới hạn)</summary>
        public int? SubjectId { get; set; }

        /// <summary>Tiêu đề phiên chat (tự tạo từ câu hỏi đầu tiên)</summary>
        public string Title { get; set; } = "New Chat";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        public int TotalMessages { get; set; } = 0;

        // Navigation
        public User User { get; set; } = null!;
        public Subject? Subject { get; set; }
        public ICollection<ChatMessage> Messages { get; set; } = [];
    }
}
