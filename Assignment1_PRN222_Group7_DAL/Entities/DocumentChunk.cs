namespace Assignment1_PRN222_Group7_DAL.Entities
{
    /// <summary>
    /// Chunk văn bản từ tài liệu sau khi xử lý.
    /// EmbeddingId là ID tham chiếu tới vector trong Chroma DB.
    /// </summary>
    public class DocumentChunk
    {
        public int Id { get; set; }

        public int DocumentId { get; set; }

        /// <summary>Thứ tự chunk trong tài liệu</summary>
        public int ChunkIndex { get; set; }

        /// <summary>Nội dung văn bản của chunk</summary>
        public string Content { get; set; } = string.Empty;

        public int ContentLength { get; set; }

        /// <summary>Trang bắt đầu trong tài liệu gốc</summary>
        public int? StartPage { get; set; }

        /// <summary>Trang kết thúc trong tài liệu gốc</summary>
        public int? EndPage { get; set; }

        /// <summary>ID vector trong Chroma DB (để tìm kiếm ngữ nghĩa)</summary>
        public string? EmbeddingId { get; set; }

        /// <summary>Tên embedding model đã dùng</summary>
        public string? EmbeddingModel { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Document Document { get; set; } = null!;
        public ICollection<MessageSource> MessageSources { get; set; } = [];
    }
}
