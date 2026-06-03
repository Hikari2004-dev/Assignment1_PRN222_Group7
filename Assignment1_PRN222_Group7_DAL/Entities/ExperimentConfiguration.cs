using Assignment1_PRN222_Group7_DAL.Enums;

namespace Assignment1_PRN222_Group7_DAL.Entities
{
    /// <summary>
    /// Cấu hình của một thí nghiệm: chunking strategy, embedding model, retrieval method.
    /// Một Experiment có thể có nhiều Configuration để so sánh.
    /// </summary>
    public class ExperimentConfiguration
    {
        public int Id { get; set; }

        public int ExperimentId { get; set; }

        /// <summary>Tên cấu hình, ví dụ: "RAG-Recursive-multilingual-e5"</summary>
        public string ConfigName { get; set; } = string.Empty;

        public ChunkingStrategy ChunkingStrategy { get; set; }

        /// <summary>Kích thước chunk (số token/ký tự)</summary>
        public int ChunkSize { get; set; } = 512;

        /// <summary>Số ký tự overlap giữa các chunk</summary>
        public int ChunkOverlap { get; set; } = 64;

        public EmbeddingModel EmbeddingModel { get; set; }

        public RetrievalMethod RetrievalMethod { get; set; }

        /// <summary>Số chunks top-K lấy ra khi tìm kiếm</summary>
        public int TopK { get; set; } = 5;

        /// <summary>Ngưỡng similarity tối thiểu (0.0 - 1.0)</summary>
        public float SimilarityThreshold { get; set; } = 0.7f;

        // Navigation
        public Experiment Experiment { get; set; } = null!;
        public ICollection<ExperimentResult> Results { get; set; } = [];
    }
}
