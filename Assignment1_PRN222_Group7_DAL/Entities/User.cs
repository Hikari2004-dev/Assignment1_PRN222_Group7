namespace Assignment1_PRN222_Group7_DAL.Entities
{
    /// <summary>
    /// Người dùng hệ thống — dùng Cookie Authentication thuần (không ASP.NET Identity).
    /// Role được tham chiếu qua bảng Roles (RoleId).
    /// </summary>
    public class User
    {
        public int Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        /// <summary>Mật khẩu đã hash (BCrypt)</summary>
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>FK → Roles.Id</summary>
        public int RoleId { get; set; }

        /// <summary>Mã sinh viên hoặc mã giảng viên (tuỳ chọn)</summary>
        public string? StudentOrStaffId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginAt { get; set; }

        // Navigation
        public Role Role { get; set; } = null!;
        public ICollection<UserSubscription> Subscriptions { get; set; } = [];
        public ICollection<ChatSession> ChatSessions { get; set; } = [];
        public ICollection<Document> UploadedDocuments { get; set; } = [];
        public ICollection<Subject> CreatedSubjects { get; set; } = [];
        public ICollection<Subject> ManagedSubjects { get; set; } = [];
        public ICollection<Experiment> Experiments { get; set; } = [];
    }
}
