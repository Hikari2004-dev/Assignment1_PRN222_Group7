using Assignment1_PRN222_Group7_DAL.Entities;

namespace Assignment1_PRN222_Group7_BLL.Services
{
    /// <summary>
    /// Interface for Experiment business logic operations.
    /// </summary>
    public interface IExperimentService
    {
        // Experiment CRUD
        Task<IEnumerable<Experiment>> GetAllExperimentsAsync();
        Task<Experiment?> GetExperimentByIdAsync(int id);
        Task<Experiment?> GetExperimentWithDetailsAsync(int id);
        Task<bool> CreateExperimentAsync(Experiment experiment);
        Task<bool> UpdateExperimentAsync(Experiment experiment);
        Task<bool> DeleteExperimentAsync(int id);

        // Configuration
        Task<ExperimentConfiguration?> GetConfigurationByIdAsync(int id);
        Task<bool> AddConfigurationAsync(ExperimentConfiguration configuration);
        Task<bool> UpdateConfigurationAsync(ExperimentConfiguration configuration);
        Task<bool> DeleteConfigurationAsync(int id);

        // TestQuestion
        Task<TestQuestion?> GetTestQuestionByIdAsync(int id);
        Task<bool> AddTestQuestionAsync(TestQuestion question);
        Task<bool> UpdateTestQuestionAsync(TestQuestion question);
        Task<bool> DeleteTestQuestionAsync(int id);

        // Experiment Run (Mock)
        Task<(bool Success, string Message)> RunExperimentAsync(int experimentId);

        // Dashboard
        Task<ExperimentDashboardData> GetDashboardDataAsync(int experimentId);
    }

    /// <summary>
    /// Data class for Dashboard metrics.
    /// </summary>
    public class ExperimentDashboardData
    {
        public Experiment? Experiment { get; set; }
        public int TotalQuestions { get; set; }
        public int TotalConfigurations { get; set; }
        public double AverageRAGASScore { get; set; }
        public double AverageLatencyMs { get; set; }
        public List<ConfigurationMetric> ConfigurationMetrics { get; set; } = new();
        public List<ExperimentResult> AllResults { get; set; } = new();
    }

    public class ConfigurationMetric
    {
        public string ConfigName { get; set; } = string.Empty;
        public double AverageRAGASScore { get; set; }
        public double AverageLatencyMs { get; set; }
    }
}
