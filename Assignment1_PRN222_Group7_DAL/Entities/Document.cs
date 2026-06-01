using Assignment1_PRN222_Group7_DAL.Enums;

namespace Assignment1_PRN222_Group7_DAL.Entities
{
    /// <summary>
    /// Tài liệu được upload: PDF, DOCX, PPTX, slide bài giảng
    /// </summary>
    public class Document
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        /// <summary>Tên file gốc khi upload</summary>
        public string OriginalFileName { get; set; } = string.Empty;

        /// <summary>Tên file sau khi lưu (unique)</summary>
        public string StoredFileName { get; set; } = string.Empty;

        /// <summary>Đường dẫn lưu file trên server</summary>
        public string FilePath { get; set; } = string.Empty;

        public FileType FileType { get; set; }

        public long FileSizeBytes { get; set; }

        public int SubjectId { get; set; }

        public int? ChapterId { get; set; }

        /// <summary>UserId của người upload</summary>
        public int UploadedBy { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Đã được chunk & embed vào vector DB chưa</summary>
        public bool IsIndexed { get; set; } = false;

        public DateTime? IndexedAt { get; set; }

        /// <summary>Tổng số chunks sau khi xử lý</summary>
        public int TotalChunks { get; set; } = 0;

        /// <summary>Model embedding đã dùng để index</summary>
        public string? EmbeddingModelUsed { get; set; }

        public string? Description { get; set; }

        // Navigation
        public Subject Subject { get; set; } = null!;
        public Chapter? Chapter { get; set; }
        public User Uploader { get; set; } = null!;
        public ICollection<DocumentChunk> Chunks { get; set; } = [];
    }
}
