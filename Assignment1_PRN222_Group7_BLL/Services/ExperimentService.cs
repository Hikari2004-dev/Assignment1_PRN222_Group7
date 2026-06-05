using Assignment1_PRN222_Group7_DAL.Entities;
using Assignment1_PRN222_Group7_DAL.Enums;
using Assignment1_PRN222_Group7_DAL.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7_BLL.Services
{
    /// <summary>
    /// Experiment business logic service.
    /// </summary>
    public class ExperimentService : IExperimentService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ExperimentService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        #region Experiment CRUD

        public async Task<IEnumerable<Experiment>> GetAllExperimentsAsync()
        {
            var repo = _unitOfWork.GetRepository<Experiment>();
            return await repo.FindAsync(e => true, "Subject", "Creator", "Configurations", "TestQuestions", "Results");
        }

        public async Task<Experiment?> GetExperimentByIdAsync(int id)
        {
            var repo = _unitOfWork.GetRepository<Experiment>();
            return await repo.FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<Experiment?> GetExperimentWithDetailsAsync(int id)
        {
            var repo = _unitOfWork.GetRepository<Experiment>();
            return await repo.FirstOrDefaultAsync(
                e => e.Id == id,
                "Subject",
                "Creator",
                "Configurations",
                "TestQuestions",
                "Results",
                "Results.Configuration",
                "Results.Question"
            );
        }

        public async Task<bool> CreateExperimentAsync(Experiment experiment)
        {
            try
            {
                experiment.CreatedAt = DateTime.UtcNow;
                experiment.Status = ExperimentStatus.Draft;

                var repo = _unitOfWork.GetRepository<Experiment>();
                await repo.AddAsync(experiment);
                await _unitOfWork.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateExperimentAsync(Experiment experiment)
        {
            try
            {
                var repo = _unitOfWork.GetRepository<Experiment>();
                repo.Update(experiment);
                await _unitOfWork.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteExperimentAsync(int id)
        {
            try
            {
                var repo = _unitOfWork.GetRepository<Experiment>();
                var experiment = await repo.GetByIdAsync(id);
                if (experiment == null) return false;

                repo.Remove(experiment);
                await _unitOfWork.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Configuration

        public async Task<ExperimentConfiguration?> GetConfigurationByIdAsync(int id)
        {
            var repo = _unitOfWork.GetRepository<ExperimentConfiguration>();
            return await repo.FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<bool> AddConfigurationAsync(ExperimentConfiguration configuration)
        {
            try
            {
                var repo = _unitOfWork.GetRepository<ExperimentConfiguration>();
                await repo.AddAsync(configuration);
                await _unitOfWork.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateConfigurationAsync(ExperimentConfiguration configuration)
        {
            try
            {
                var repo = _unitOfWork.GetRepository<ExperimentConfiguration>();
                repo.Update(configuration);
                await _unitOfWork.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteConfigurationAsync(int id)
        {
            try
            {
                var repo = _unitOfWork.GetRepository<ExperimentConfiguration>();
                var config = await repo.GetByIdAsync(id);
                if (config == null) return false;

                repo.Remove(config);
                await _unitOfWork.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region TestQuestion

        public async Task<TestQuestion?> GetTestQuestionByIdAsync(int id)
        {
            var repo = _unitOfWork.GetRepository<TestQuestion>();
            return await repo.FirstOrDefaultAsync(q => q.Id == id);
        }

        public async Task<bool> AddTestQuestionAsync(TestQuestion question)
        {
            try
            {
                question.CreatedAt = DateTime.UtcNow;
                var repo = _unitOfWork.GetRepository<TestQuestion>();
                await repo.AddAsync(question);
                await _unitOfWork.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateTestQuestionAsync(TestQuestion question)
        {
            try
            {
                var repo = _unitOfWork.GetRepository<TestQuestion>();
                repo.Update(question);
                await _unitOfWork.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteTestQuestionAsync(int id)
        {
            try
            {
                var repo = _unitOfWork.GetRepository<TestQuestion>();
                var question = await repo.GetByIdAsync(id);
                if (question == null) return false;

                repo.Remove(question);
                await _unitOfWork.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Run Experiment (Mock)

        public async Task<(bool Success, string Message)> RunExperimentAsync(int experimentId)
        {
            var experiment = await GetExperimentWithDetailsAsync(experimentId);

            if (experiment == null)
            {
                return (false, "Experiment not found.");
            }

            if (!experiment.TestQuestions.Any())
            {
                return (false, "Cannot run experiment without test questions. Please add at least one test question.");
            }

            if (!experiment.Configurations.Any())
            {
                return (false, "Cannot run experiment without configurations. Please add at least one configuration.");
            }

            // Delete old results
            var resultRepo = _unitOfWork.GetRepository<ExperimentResult>();
            var oldResults = await resultRepo.FindAsync(r => r.ExperimentId == experimentId);
            if (oldResults.Any())
            {
                resultRepo.RemoveRange(oldResults);
                await _unitOfWork.SaveChangesAsync();
            }

            var random = new Random();
            var results = new List<ExperimentResult>();

            // Run for each configuration and question
            foreach (var config in experiment.Configurations)
            {
                foreach (var question in experiment.TestQuestions.OrderBy(q => q.OrderIndex))
                {
                    var contextPrecision = (float)(random.NextDouble() * 0.35 + 0.6); // 0.6 - 0.95
                    var contextRecall = (float)(random.NextDouble() * 0.35 + 0.6);
                    var faithfulness = (float)(random.NextDouble() * 0.35 + 0.6);
                    var answerRelevancy = (float)(random.NextDouble() * 0.35 + 0.6);
                    var ragasScore = (contextPrecision + contextRecall + faithfulness + answerRelevancy) / 4;
                    var latencyMs = random.Next(500, 3001); // 500 - 3000

                    var result = new ExperimentResult
                    {
                        ExperimentId = experimentId,
                        ConfigId = config.Id,
                        QuestionId = question.Id,
                        GeneratedAnswer = $"Mock answer for: {question.Question}",
                        RetrievedContexts = question.ReferenceContext,
                        ContextPrecision = (float)Math.Round(contextPrecision, 4),
                        ContextRecall = (float)Math.Round(contextRecall, 4),
                        Faithfulness = (float)Math.Round(faithfulness, 4),
                        AnswerRelevancy = (float)Math.Round(answerRelevancy, 4),
                        RAGASScore = (float)Math.Round(ragasScore, 4),
                        LatencyMs = latencyMs,
                        CreatedAt = DateTime.UtcNow
                    };

                    results.Add(result);
                }
            }

            // Add all results
            await resultRepo.AddRangeAsync(results);
            await _unitOfWork.SaveChangesAsync();

            // Update experiment status
            experiment.Status = ExperimentStatus.Completed;
            experiment.CompletedAt = DateTime.UtcNow;
            var expRepo = _unitOfWork.GetRepository<Experiment>();
            expRepo.Update(experiment);
            await _unitOfWork.SaveChangesAsync();

            return (true, $"Experiment completed successfully. Generated {results.Count} results.");
        }

        #endregion

        #region Dashboard

        public async Task<ExperimentDashboardData> GetDashboardDataAsync(int experimentId)
        {
            var experiment = await GetExperimentWithDetailsAsync(experimentId);
            if (experiment == null)
            {
                return new ExperimentDashboardData();
            }

            var resultRepo = _unitOfWork.GetRepository<ExperimentResult>();
            var results = await resultRepo.FindAsync(
                r => r.ExperimentId == experimentId,
                "Configuration",
                "Question"
            );

            var resultsList = results.ToList();

            var data = new ExperimentDashboardData
            {
                Experiment = experiment,
                TotalQuestions = experiment.TestQuestions.Count,
                TotalConfigurations = experiment.Configurations.Count,
                AverageRAGASScore = resultsList.Any()
                    ? Math.Round(resultsList.Average(r => r.RAGASScore ?? 0), 4)
                    : 0,
                AverageLatencyMs = resultsList.Any()
                    ? Math.Round(resultsList.Average(r => r.LatencyMs), 2)
                    : 0,
                AllResults = resultsList
            };

            // Calculate metrics per configuration
            if (resultsList.Any())
            {
                var configGroups = resultsList.GroupBy(r => r.Configuration?.ConfigName ?? "Unknown");

                foreach (var group in configGroups)
                {
                    data.ConfigurationMetrics.Add(new ConfigurationMetric
                    {
                        ConfigName = group.Key,
                        AverageRAGASScore = Math.Round(group.Average(r => r.RAGASScore ?? 0), 4),
                        AverageLatencyMs = Math.Round(group.Average(r => r.LatencyMs), 2)
                    });
                }
            }

            return data;
        }

        #endregion
    }
}
