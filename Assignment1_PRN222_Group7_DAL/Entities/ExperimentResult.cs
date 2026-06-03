namespace Assignment1_PRN222_Group7_DAL.Entities
{
    /// <summary>
    /// Kết quả thí nghiệm với RAGAS metrics.
    /// Bảng số liệu để so sánh chunking/embedding strategies.
    /// </summary>
    public class ExperimentResult
    {
        public int Id { get; set; }

        public int ExperimentId { get; set; }

        public int ConfigId { get; set; }

        public int QuestionId { get; set; }

        /// <summary>Câu trả lời do AI sinh ra</summary>
        public string GeneratedAnswer { get; set; } = string.Empty;

        /// <summary>Chunks được truy xuất (JSON array of chunk contents)</summary>
        public string? RetrievedContexts { get; set; }

        // RAGAS Metrics (0.0 - 1.0)
        /// <summary>Context Precision: độ chính xác của context truy xuất</summary>
        public float? ContextPrecision { get; set; }

        /// <summary>Context Recall: độ bao phủ context so với ground truth</summary>
        public float? ContextRecall { get; set; }

        /// <summary>Faithfulness: câu trả lời có trung thực với context không</summary>
        public float? Faithfulness { get; set; }

        /// <summary>Answer Relevancy: câu trả lời có liên quan tới câu hỏi không</summary>
        public float? AnswerRelevancy { get; set; }

        /// <summary>RAGAS Score tổng hợp</summary>
        public float? RAGASScore { get; set; }

        /// <summary>Thời gian xử lý (milliseconds)</summary>
        public int LatencyMs { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Experiment Experiment { get; set; } = null!;
        public ExperimentConfiguration Configuration { get; set; } = null!;
        public TestQuestion Question { get; set; } = null!;
    }
}
