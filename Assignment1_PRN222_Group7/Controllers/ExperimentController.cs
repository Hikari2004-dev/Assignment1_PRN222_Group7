using Assignment1_PRN222_Group7_BLL.Services;
using Assignment1_PRN222_Group7_DAL.Entities;
using Assignment1_PRN222_Group7_DAL.Enums;
using Assignment1_PRN222_Group7_DAL.Repositories;
using Assignment1_PRN222_Group7.Models.ExperimentViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7.Controllers
{
    [Authorize]
    public class ExperimentController : Controller
    {
        private readonly IExperimentService _experimentService;
        private readonly IUnitOfWork _unitOfWork;

        public ExperimentController(IExperimentService experimentService, IUnitOfWork unitOfWork)
        {
            _experimentService = experimentService;
            _unitOfWork = unitOfWork;
        }

        private int GetUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private string GetUserRole() =>
            User.FindFirstValue(ClaimTypes.Role) ?? "Student";

        private bool IsAdminOrLecturer() =>
            GetUserRole() == "Admin" || GetUserRole() == "Lecturer";

        #region Index

        // GET: /Experiment
        public async Task<IActionResult> Index()
        {
            var experiments = await _experimentService.GetAllExperimentsAsync();
            return View(experiments);
        }

        #endregion

        #region Details

        // GET: /Experiment/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var experiment = await _experimentService.GetExperimentWithDetailsAsync(id);
            if (experiment == null)
            {
                return NotFound();
            }

            var vm = new ExperimentDetailViewModel
            {
                Id = experiment.Id,
                Name = experiment.Name,
                Description = experiment.Description,
                SubjectId = experiment.SubjectId,
                SubjectName = experiment.Subject?.Name ?? "Unknown",
                CreatedBy = experiment.CreatedBy,
                CreatorName = experiment.Creator?.FullName ?? "Unknown",
                CreatedAt = experiment.CreatedAt,
                CompletedAt = experiment.CompletedAt,
                Status = experiment.Status.ToString(),
                ConfigurationCount = experiment.Configurations.Count,
                TestQuestionCount = experiment.TestQuestions.Count,
                ResultCount = experiment.Results.Count,
                Configurations = experiment.Configurations.Select(c => new ExperimentConfigurationViewModel
                {
                    Id = c.Id,
                    ExperimentId = c.ExperimentId,
                    ConfigName = c.ConfigName,
                    ChunkingStrategy = c.ChunkingStrategy.ToString(),
                    ChunkSize = c.ChunkSize,
                    ChunkOverlap = c.ChunkOverlap,
                    EmbeddingModel = c.EmbeddingModel.ToString(),
                    RetrievalMethod = c.RetrievalMethod.ToString(),
                    TopK = c.TopK,
                    SimilarityThreshold = c.SimilarityThreshold
                }).ToList(),
                TestQuestions = experiment.TestQuestions.OrderBy(q => q.OrderIndex).Select(q => new TestQuestionViewModel
                {
                    Id = q.Id,
                    ExperimentId = q.ExperimentId,
                    Question = q.Question,
                    GroundTruth = q.GroundTruth,
                    ReferenceContext = q.ReferenceContext,
                    OrderIndex = q.OrderIndex
                }).ToList()
            };

            return View(vm);
        }

        #endregion

        #region Create

        // GET: /Experiment/Create
        [Authorize(Policy = "LecturerUp")]
        public async Task<IActionResult> Create()
        {
            var subjectRepo = _unitOfWork.GetRepository<Subject>();
            var subjects = await subjectRepo.FindAsync(s => s.IsActive);

            var vm = new ExperimentCreateViewModel
            {
                AvailableSubjects = subjects.ToList()
            };

            return View(vm);
        }

        // POST: /Experiment/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "LecturerUp")]
        public async Task<IActionResult> Create(string name, string? description, int subjectId)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                ModelState.AddModelError("Name", "Experiment name is required.");
                return await Create();
            }

            if (subjectId <= 0)
            {
                ModelState.AddModelError("SubjectId", "Please select a subject.");
                return await Create();
            }

            var experiment = new Experiment
            {
                Name = name.Trim(),
                Description = description?.Trim(),
                SubjectId = subjectId,
                CreatedBy = GetUserId(),
                Status = ExperimentStatus.Draft
            };

            var success = await _experimentService.CreateExperimentAsync(experiment);
            if (!success)
            {
                ModelState.AddModelError("", "Failed to create experiment.");
                return await Create();
            }

            TempData["Success"] = $"Experiment \"{experiment.Name}\" created successfully.";
            return RedirectToAction(nameof(Details), new { id = experiment.Id });
        }

        #endregion

        #region Edit

        // GET: /Experiment/Edit/5
        [Authorize(Policy = "LecturerUp")]
        public async Task<IActionResult> Edit(int id)
        {
            var experiment = await _experimentService.GetExperimentByIdAsync(id);
            if (experiment == null)
            {
                return NotFound();
            }

            var subjectRepo = _unitOfWork.GetRepository<Subject>();
            var subjects = await subjectRepo.FindAsync(s => s.IsActive);

            var vm = new ExperimentEditViewModel
            {
                Id = experiment.Id,
                Name = experiment.Name,
                Description = experiment.Description,
                SubjectId = experiment.SubjectId,
                CreatedAt = experiment.CreatedAt,
                Status = experiment.Status.ToString(),
                AvailableSubjects = subjects.ToList()
            };

            return View(vm);
        }

        // POST: /Experiment/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "LecturerUp")]
        public async Task<IActionResult> Edit(int id, string name, string? description, int subjectId)
        {
            var experiment = await _experimentService.GetExperimentByIdAsync(id);
            if (experiment == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                ModelState.AddModelError("Name", "Experiment name is required.");
                return await Edit(id);
            }

            experiment.Name = name.Trim();
            experiment.Description = description?.Trim();
            experiment.SubjectId = subjectId;

            var success = await _experimentService.UpdateExperimentAsync(experiment);
            if (!success)
            {
                ModelState.AddModelError("", "Failed to update experiment.");
                return await Edit(id);
            }

            TempData["Success"] = $"Experiment \"{experiment.Name}\" updated successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }

        #endregion

        #region Delete

        // GET: /Experiment/Delete/5
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int id)
        {
            var experiment = await _experimentService.GetExperimentByIdAsync(id);
            if (experiment == null)
            {
                return NotFound();
            }

            return View(experiment);
        }

        // POST: /Experiment/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var experiment = await _experimentService.GetExperimentByIdAsync(id);
            var name = experiment?.Name ?? "Experiment";

            var success = await _experimentService.DeleteExperimentAsync(id);
            if (!success)
            {
                TempData["Error"] = "Failed to delete experiment.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = $"Experiment \"{name}\" deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Configuration

        // GET: /Experiment/AddConfiguration?experimentId=5
        [Authorize(Policy = "LecturerUp")]
        public async Task<IActionResult> AddConfiguration(int experimentId)
        {
            var experiment = await _experimentService.GetExperimentByIdAsync(experimentId);
            if (experiment == null)
            {
                return NotFound();
            }

            var vm = new ConfigurationFormViewModel
            {
                ExperimentId = experimentId
            };

            ViewBag.ExperimentName = experiment.Name;
            return View(vm);
        }

        // POST: /Experiment/AddConfiguration
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "LecturerUp")]
        public async Task<IActionResult> AddConfiguration(ConfigurationFormViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.ConfigName))
            {
                ModelState.AddModelError("ConfigName", "Configuration name is required.");
                return await AddConfiguration(model.ExperimentId);
            }

            var config = new ExperimentConfiguration
            {
                ExperimentId = model.ExperimentId,
                ConfigName = model.ConfigName.Trim(),
                ChunkingStrategy = Enum.Parse<ChunkingStrategy>(model.ChunkingStrategy),
                ChunkSize = model.ChunkSize,
                ChunkOverlap = model.ChunkOverlap,
                EmbeddingModel = Enum.Parse<EmbeddingModel>(model.EmbeddingModel),
                RetrievalMethod = Enum.Parse<RetrievalMethod>(model.RetrievalMethod),
                TopK = model.TopK,
                SimilarityThreshold = model.SimilarityThreshold
            };

            var success = await _experimentService.AddConfigurationAsync(config);
            if (!success)
            {
                TempData["Error"] = "Failed to add configuration.";
                return RedirectToAction(nameof(Details), new { id = model.ExperimentId });
            }

            TempData["Success"] = $"Configuration \"{config.ConfigName}\" added successfully.";
            return RedirectToAction(nameof(Details), new { id = model.ExperimentId });
        }

        // GET: /Experiment/EditConfiguration/5
        [Authorize(Policy = "LecturerUp")]
        public async Task<IActionResult> EditConfiguration(int id)
        {
            var config = await _experimentService.GetConfigurationByIdAsync(id);
            if (config == null)
            {
                return NotFound();
            }

            var vm = new ConfigurationFormViewModel
            {
                Id = config.Id,
                ExperimentId = config.ExperimentId,
                ConfigName = config.ConfigName,
                ChunkingStrategy = config.ChunkingStrategy.ToString(),
                ChunkSize = config.ChunkSize,
                ChunkOverlap = config.ChunkOverlap,
                EmbeddingModel = config.EmbeddingModel.ToString(),
                RetrievalMethod = config.RetrievalMethod.ToString(),
                TopK = config.TopK,
                SimilarityThreshold = config.SimilarityThreshold
            };

            return View(vm);
        }

        // POST: /Experiment/EditConfiguration/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "LecturerUp")]
        public async Task<IActionResult> EditConfiguration(ConfigurationFormViewModel model)
        {
            var config = await _experimentService.GetConfigurationByIdAsync(model.Id);
            if (config == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(model.ConfigName))
            {
                ModelState.AddModelError("ConfigName", "Configuration name is required.");
                return View(model);
            }

            config.ConfigName = model.ConfigName.Trim();
            config.ChunkingStrategy = Enum.Parse<ChunkingStrategy>(model.ChunkingStrategy);
            config.ChunkSize = model.ChunkSize;
            config.ChunkOverlap = model.ChunkOverlap;
            config.EmbeddingModel = Enum.Parse<EmbeddingModel>(model.EmbeddingModel);
            config.RetrievalMethod = Enum.Parse<RetrievalMethod>(model.RetrievalMethod);
            config.TopK = model.TopK;
            config.SimilarityThreshold = model.SimilarityThreshold;

            var success = await _experimentService.UpdateConfigurationAsync(config);
            if (!success)
            {
                TempData["Error"] = "Failed to update configuration.";
                return RedirectToAction(nameof(Details), new { id = model.ExperimentId });
            }

            TempData["Success"] = $"Configuration \"{config.ConfigName}\" updated successfully.";
            return RedirectToAction(nameof(Details), new { id = model.ExperimentId });
        }

        // POST: /Experiment/DeleteConfiguration
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "LecturerUp")]
        public async Task<IActionResult> DeleteConfiguration(int id, int experimentId)
        {
            var success = await _experimentService.DeleteConfigurationAsync(id);
            if (!success)
            {
                TempData["Error"] = "Failed to delete configuration.";
            }
            else
            {
                TempData["Success"] = "Configuration deleted successfully.";
            }

            return RedirectToAction(nameof(Details), new { id = experimentId });
        }

        #endregion

        #region TestQuestion

        // GET: /Experiment/AddTestQuestion?experimentId=5
        [Authorize(Policy = "LecturerUp")]
        public async Task<IActionResult> AddTestQuestion(int experimentId)
        {
            var experiment = await _experimentService.GetExperimentByIdAsync(experimentId);
            if (experiment == null)
            {
                return NotFound();
            }

            // Get max order index
            var detail = await _experimentService.GetExperimentWithDetailsAsync(experimentId);
            var maxOrder = detail?.TestQuestions?.Any() == true
                ? detail.TestQuestions.Max(q => q.OrderIndex)
                : 0;

            var vm = new TestQuestionFormViewModel
            {
                ExperimentId = experimentId,
                OrderIndex = maxOrder + 1
            };

            ViewBag.ExperimentName = experiment.Name;
            return View(vm);
        }

        // POST: /Experiment/AddTestQuestion
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "LecturerUp")]
        public async Task<IActionResult> AddTestQuestion(TestQuestionFormViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Question))
            {
                ModelState.AddModelError("Question", "Question text is required.");
                return await AddTestQuestion(model.ExperimentId);
            }

            if (string.IsNullOrWhiteSpace(model.GroundTruth))
            {
                ModelState.AddModelError("GroundTruth", "Ground truth answer is required.");
                return await AddTestQuestion(model.ExperimentId);
            }

            var question = new TestQuestion
            {
                ExperimentId = model.ExperimentId,
                Question = model.Question.Trim(),
                GroundTruth = model.GroundTruth.Trim(),
                ReferenceContext = model.ReferenceContext?.Trim(),
                OrderIndex = model.OrderIndex
            };

            var success = await _experimentService.AddTestQuestionAsync(question);
            if (!success)
            {
                TempData["Error"] = "Failed to add test question.";
                return RedirectToAction(nameof(Details), new { id = model.ExperimentId });
            }

            TempData["Success"] = "Test question added successfully.";
            return RedirectToAction(nameof(Details), new { id = model.ExperimentId });
        }

        // GET: /Experiment/EditTestQuestion/5
        [Authorize(Policy = "LecturerUp")]
        public async Task<IActionResult> EditTestQuestion(int id)
        {
            var question = await _experimentService.GetTestQuestionByIdAsync(id);
            if (question == null)
            {
                return NotFound();
            }

            var vm = new TestQuestionFormViewModel
            {
                Id = question.Id,
                ExperimentId = question.ExperimentId,
                Question = question.Question,
                GroundTruth = question.GroundTruth,
                ReferenceContext = question.ReferenceContext,
                OrderIndex = question.OrderIndex
            };

            return View(vm);
        }

        // POST: /Experiment/EditTestQuestion/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "LecturerUp")]
        public async Task<IActionResult> EditTestQuestion(TestQuestionFormViewModel model)
        {
            var question = await _experimentService.GetTestQuestionByIdAsync(model.Id);
            if (question == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(model.Question))
            {
                ModelState.AddModelError("Question", "Question text is required.");
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.GroundTruth))
            {
                ModelState.AddModelError("GroundTruth", "Ground truth answer is required.");
                return View(model);
            }

            question.Question = model.Question.Trim();
            question.GroundTruth = model.GroundTruth.Trim();
            question.ReferenceContext = model.ReferenceContext?.Trim();
            question.OrderIndex = model.OrderIndex;

            var success = await _experimentService.UpdateTestQuestionAsync(question);
            if (!success)
            {
                TempData["Error"] = "Failed to update test question.";
                return RedirectToAction(nameof(Details), new { id = model.ExperimentId });
            }

            TempData["Success"] = "Test question updated successfully.";
            return RedirectToAction(nameof(Details), new { id = model.ExperimentId });
        }

        // POST: /Experiment/DeleteTestQuestion
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "LecturerUp")]
        public async Task<IActionResult> DeleteTestQuestion(int id, int experimentId)
        {
            var success = await _experimentService.DeleteTestQuestionAsync(id);
            if (!success)
            {
                TempData["Error"] = "Failed to delete test question.";
            }
            else
            {
                TempData["Success"] = "Test question deleted successfully.";
            }

            return RedirectToAction(nameof(Details), new { id = experimentId });
        }

        #endregion

        #region Run Experiment

        // POST: /Experiment/RunExperiment
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "LecturerUp")]
        public async Task<IActionResult> RunExperiment(int id)
        {
            var (success, message) = await _experimentService.RunExperimentAsync(id);

            if (!success)
            {
                TempData["Error"] = message;
                return RedirectToAction(nameof(Details), new { id });
            }

            TempData["Success"] = message;
            return RedirectToAction(nameof(Dashboard), new { id });
        }

        #endregion

        #region Dashboard

        // GET: /Experiment/Dashboard/5
        public async Task<IActionResult> Dashboard(int id)
        {
            var data = await _experimentService.GetDashboardDataAsync(id);
            if (data.Experiment == null)
            {
                return NotFound();
            }

            var vm = new ExperimentDashboardViewModel
            {
                ExperimentId = id,
                ExperimentName = data.Experiment.Name,
                TotalQuestions = data.TotalQuestions,
                TotalConfigurations = data.TotalConfigurations,
                AverageRAGASScore = data.AverageRAGASScore,
                AverageLatencyMs = data.AverageLatencyMs,
                ConfigurationMetrics = data.ConfigurationMetrics.Select(m => new ConfigurationMetricViewModel
                {
                    ConfigName = m.ConfigName,
                    AverageRAGASScore = m.AverageRAGASScore,
                    AverageLatencyMs = m.AverageLatencyMs
                }).ToList(),
                AllResults = data.AllResults.Select(r => new ExperimentResultViewModel
                {
                    Id = r.Id,
                    ExperimentId = r.ExperimentId,
                    ConfigId = r.ConfigId,
                    ConfigName = r.Configuration?.ConfigName ?? "Unknown",
                    QuestionId = r.QuestionId,
                    QuestionText = r.Question?.Question ?? "Unknown",
                    GeneratedAnswer = r.GeneratedAnswer,
                    RetrievedContexts = r.RetrievedContexts,
                    ContextPrecision = r.ContextPrecision,
                    ContextRecall = r.ContextRecall,
                    Faithfulness = r.Faithfulness,
                    AnswerRelevancy = r.AnswerRelevancy,
                    RAGASScore = r.RAGASScore,
                    LatencyMs = r.LatencyMs,
                    CreatedAt = r.CreatedAt
                }).ToList()
            };

            return View(vm);
        }

        #endregion
    }
}
