using Assignment1_PRN222_Group7_BLL.Services;
using Assignment1_PRN222_Group7_DAL.Entities;
using Assignment1_PRN222_Group7.Models.ExperimentViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7.Pages.Experiment
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly IExperimentService _experimentService;

        public DetailsModel(IExperimentService experimentService)
        {
            _experimentService = experimentService;
        }

        public ExperimentDetailViewModel Experiment { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var experiment = await _experimentService.GetExperimentWithDetailsAsync(id);
            if (experiment == null)
            {
                return NotFound();
            }

            Experiment = new ExperimentDetailViewModel
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

            return Page();
        }

        public async Task<IActionResult> OnPostDeleteConfigurationAsync(int id, int experimentId)
        {
            if (!User.IsInRole("Admin") && !User.IsInRole("Lecturer")) return Forbid();

            var success = await _experimentService.DeleteConfigurationAsync(id);
            if (!success)
            {
                TempData["Error"] = "Failed to delete configuration.";
            }
            else
            {
                TempData["Success"] = "Configuration deleted successfully.";
            }

            return RedirectToPage("/Experiment/Details", new { id = experimentId });
        }

        public async Task<IActionResult> OnPostDeleteTestQuestionAsync(int id, int experimentId)
        {
            if (!User.IsInRole("Admin") && !User.IsInRole("Lecturer")) return Forbid();

            var success = await _experimentService.DeleteTestQuestionAsync(id);
            if (!success)
            {
                TempData["Error"] = "Failed to delete test question.";
            }
            else
            {
                TempData["Success"] = "Test question deleted successfully.";
            }

            return RedirectToPage("/Experiment/Details", new { id = experimentId });
        }

        public async Task<IActionResult> OnPostRunExperimentAsync(int id)
        {
            if (!User.IsInRole("Admin") && !User.IsInRole("Lecturer")) return Forbid();

            var (success, message) = await _experimentService.RunExperimentAsync(id);

            if (!success)
            {
                TempData["Error"] = message;
                return RedirectToPage("/Experiment/Details", new { id });
            }

            TempData["Success"] = message;
            return RedirectToPage("/Experiment/Dashboard", new { id });
        }
    }
}
