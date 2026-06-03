namespace Assignment1_PRN222_Group7_DAL.Entities
{
    /// <summary>
    /// Bộ câu hỏi kiểm tra với ground truth.
    /// Test set 50 câu hỏi + câu trả lời đúng chuẩn bị sẵn bởi con người.
    /// </summary>
    public class TestQuestion
    {
        public int Id { get; set; }

        public int ExperimentId { get; set; }

        public string Question { get; set; } = string.Empty;

        /// <summary>Câu trả lời đúng được chuẩn bị bởi con người (ground truth)</summary>
        public string GroundTruth { get; set; } = string.Empty;

        /// <summary>Ngữ cảnh tham chiếu (optional)</summary>
        public string? ReferenceContext { get; set; }

        public int OrderIndex { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public Experiment Experiment { get; set; } = null!;
        public ICollection<ExperimentResult> Results { get; set; } = [];
    }
}
