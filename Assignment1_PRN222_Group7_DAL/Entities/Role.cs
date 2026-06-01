namespace Assignment1_PRN222_Group7_DAL.Entities
{
    /// <summary>
    /// Bảng Roles mô tả các vai trò trong hệ thống.
    /// Seed: Student (1), Lecturer (2), Admin (3)
    /// </summary>
    public class Role
    {
        public int Id { get; set; }

        /// <summary>"Student" | "Lecturer" | "Admin"</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Dùng để so sánh không phân biệt hoa thường</summary>
        public string NormalizedName { get; set; } = string.Empty;

        public string? Description { get; set; }

        // Navigation
        public ICollection<User> Users { get; set; } = [];
    }
}
