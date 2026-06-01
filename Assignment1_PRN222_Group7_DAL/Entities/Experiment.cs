using Assignment1_PRN222_Group7_DAL.Enums;

namespace Assignment1_PRN222_Group7_DAL.Entities
{
    /// <summary>
    /// Thí nghiệm so sánh RAG vs Fine-tuned, benchmark chunking/embedding.
    /// Module nghiên cứu (RBL).
    /// </summary>
    public class Experiment
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int SubjectId { get; set; }

        /// <summary>UserId của người tạo thí nghiệm (Lecturer/Admin)</summary>
        public int CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CompletedAt { get; set; }

        public ExperimentStatus Status { get; set; } = ExperimentStatus.Draft;

        // Navigation
        public Subject Subject { get; set; } = null!;
        public User Creator { get; set; } = null!;
        public ICollection<ExperimentConfiguration> Configurations { get; set; } = [];
        public ICollection<TestQuestion> TestQuestions { get; set; } = [];
        public ICollection<ExperimentResult> Results { get; set; } = [];
    }
}
