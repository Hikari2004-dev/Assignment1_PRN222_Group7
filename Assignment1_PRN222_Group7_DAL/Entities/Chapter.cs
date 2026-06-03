namespace Assignment1_PRN222_Group7_DAL.Entities
{
    /// <summary>Chương/phần trong môn học</summary>
    public class Chapter
    {
        public int Id { get; set; }

        public int SubjectId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        /// <summary>Thứ tự chương trong môn học</summary>
        public int OrderIndex { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Subject Subject { get; set; } = null!;
        public ICollection<Document> Documents { get; set; } = [];
    }
}
