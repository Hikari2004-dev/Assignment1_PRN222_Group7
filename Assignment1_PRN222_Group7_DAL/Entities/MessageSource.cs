namespace Assignment1_PRN222_Group7_DAL.Entities
{
    /// <summary>
    /// Nguồn trích dẫn của câu trả lời từ AI.
    /// Liên kết message với chunk tài liệu được sử dụng để trả lời.
    /// </summary>
    public class MessageSource
    {
        public int Id { get; set; }

        public int MessageId { get; set; }

        public int ChunkId { get; set; }

        /// <summary>Điểm tương đồng semantic (0.0 - 1.0)</summary>
        public float SimilarityScore { get; set; }

        /// <summary>Đoạn văn bản trích dẫn từ chunk</summary>
        public string CitedContent { get; set; } = string.Empty;

        /// <summary>Thứ tự nguồn trong câu trả lời</summary>
        public int SourceIndex { get; set; }

        // Navigation
        public ChatMessage Message { get; set; } = null!;
        public DocumentChunk Chunk { get; set; } = null!;
    }
}
