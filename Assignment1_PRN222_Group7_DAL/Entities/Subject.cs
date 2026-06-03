namespace Assignment1_PRN222_Group7_DAL.Entities
{
    /// <summary>Môn học — quản lý tài liệu theo môn</summary>
    public class Subject
    {
        public int Id { get; set; }

        /// <summary>Mã môn học, ví dụ: PRN222, SWP391</summary>
        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        /// <summary>UserId của giảng viên tạo môn</summary>
        public int? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        // Navigation
        public User? Creator { get; set; }
        public ICollection<Chapter> Chapters { get; set; } = [];
        public ICollection<Document> Documents { get; set; } = [];
        public ICollection<ChatSession> ChatSessions { get; set; } = [];
        public ICollection<Experiment> Experiments { get; set; } = [];
    }
}
