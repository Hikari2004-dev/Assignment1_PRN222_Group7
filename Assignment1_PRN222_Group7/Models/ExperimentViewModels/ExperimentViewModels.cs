using System;
using System.Collections.Generic;
using Assignment1_PRN222_Group7_DAL.Entities;

namespace Assignment1_PRN222_Group7.Models.ExperimentViewModels
{
    /// <summary>
    /// ViewModel for creating a new experiment.
    /// </summary>
    public class ExperimentCreateViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int SubjectId { get; set; }
        public List<Subject> AvailableSubjects { get; set; } = new();
    }

    /// <summary>
    /// ViewModel for editing an existing experiment.
    /// </summary>
    public class ExperimentEditViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int SubjectId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<Subject> AvailableSubjects { get; set; } = new();
    }

    /// <summary>
    /// ViewModel for displaying experiment details.
    /// </summary>
    public class ExperimentDetailViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public int CreatedBy { get; set; }
        public string CreatorName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public int ConfigurationCount { get; set; }
        public int TestQuestionCount { get; set; }
        public int ResultCount { get; set; }
        public List<ExperimentConfigurationViewModel> Configurations { get; set; } = new();
        public List<TestQuestionViewModel> TestQuestions { get; set; } = new();
    }

    /// <summary>
    /// ViewModel for experiment configuration.
    /// </summary>
    public class ExperimentConfigurationViewModel
    {
        public int Id { get; set; }
        public int ExperimentId { get; set; }
        public string ConfigName { get; set; } = string.Empty;
        public string ChunkingStrategy { get; set; } = string.Empty;
        public int ChunkSize { get; set; }
        public int ChunkOverlap { get; set; }
        public string EmbeddingModel { get; set; } = string.Empty;
        public string RetrievalMethod { get; set; } = string.Empty;
        public int TopK { get; set; }
        public float SimilarityThreshold { get; set; }
    }

    /// <summary>
    /// ViewModel for test question.
    /// </summary>
    public class TestQuestionViewModel
    {
        public int Id { get; set; }
        public int ExperimentId { get; set; }
        public string Question { get; set; } = string.Empty;
        public string GroundTruth { get; set; } = string.Empty;
        public string? ReferenceContext { get; set; }
        public int OrderIndex { get; set; }
    }

    /// <summary>
    /// ViewModel for experiment result.
    /// </summary>
    public class ExperimentResultViewModel
    {
        public int Id { get; set; }
        public int ExperimentId { get; set; }
        public int ConfigId { get; set; }
        public string ConfigName { get; set; } = string.Empty;
        public int QuestionId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string GeneratedAnswer { get; set; } = string.Empty;
        public string? RetrievedContexts { get; set; }
        public float? ContextPrecision { get; set; }
        public float? ContextRecall { get; set; }
        public float? Faithfulness { get; set; }
        public float? AnswerRelevancy { get; set; }
        public float? RAGASScore { get; set; }
        public int LatencyMs { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// ViewModel for experiment dashboard.
    /// </summary>
    public class ExperimentDashboardViewModel
    {
        public int ExperimentId { get; set; }
        public string ExperimentName { get; set; } = string.Empty;
        public int TotalQuestions { get; set; }
        public int TotalConfigurations { get; set; }
        public double AverageRAGASScore { get; set; }
        public double AverageLatencyMs { get; set; }
        public List<ConfigurationMetricViewModel> ConfigurationMetrics { get; set; } = new();
        public List<ExperimentResultViewModel> AllResults { get; set; } = new();
    }

    /// <summary>
    /// ViewModel for configuration metrics on dashboard.
    /// </summary>
    public class ConfigurationMetricViewModel
    {
        public string ConfigName { get; set; } = string.Empty;
        public double AverageRAGASScore { get; set; }
        public double AverageLatencyMs { get; set; }
    }

    /// <summary>
    /// ViewModel for adding/editing configuration.
    /// </summary>
    public class ConfigurationFormViewModel
    {
        public int Id { get; set; }
        public int ExperimentId { get; set; }
        public string ConfigName { get; set; } = string.Empty;
        public string ChunkingStrategy { get; set; } = "Fixed";
        public int ChunkSize { get; set; } = 512;
        public int ChunkOverlap { get; set; } = 64;
        public string EmbeddingModel { get; set; } = "MultilingualE5Base";
        public string RetrievalMethod { get; set; } = "RAG";
        public int TopK { get; set; } = 5;
        public float SimilarityThreshold { get; set; } = 0.7f;
    }

    /// <summary>
    /// ViewModel for adding/editing test question.
    /// </summary>
    public class TestQuestionFormViewModel
    {
        public int Id { get; set; }
        public int ExperimentId { get; set; }
        public string Question { get; set; } = string.Empty;
        public string GroundTruth { get; set; } = string.Empty;
        public string? ReferenceContext { get; set; }
        public int OrderIndex { get; set; }
    }
}
