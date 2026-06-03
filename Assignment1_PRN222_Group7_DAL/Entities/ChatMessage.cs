using Assignment1_PRN222_Group7_DAL.Enums;

namespace Assignment1_PRN222_Group7_DAL.Entities
{
    /// <summary>Tin nhắn trong phiên chat</summary>
    public class ChatMessage
    {
        public int Id { get; set; }

        public int SessionId { get; set; }

        /// <summary>User | Assistant | System</summary>
        public MessageRole Role { get; set; }

        /// <summary>Nội dung tin nhắn</summary>
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Số token đã dùng (cho assistant messages)</summary>
        public int? TokensUsed { get; set; }

        /// <summary>RAG | FineTuned | Hybrid — phương pháp tìm kiếm</summary>
        public RetrievalMethod? RetrievalMethod { get; set; }

        /// <summary>Thời gian xử lý (ms)</summary>
        public int? ProcessingTimeMs { get; set; }

        // Navigation
        public ChatSession Session { get; set; } = null!;
        public ICollection<MessageSource> Sources { get; set; } = [];
    }
}
