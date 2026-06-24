using Assignment1_PRN222_Group7_BLL.Services;
using Assignment1_PRN222_Group7.Models.ExperimentViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Linq;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7.Pages.Experiment
{
    [Authorize]
    public class DashboardModel : PageModel
    {
        private readonly IExperimentService _experimentService;

        public DashboardModel(IExperimentService experimentService)
        {
            _experimentService = experimentService;
        }

        public ExperimentDashboardViewModel Dashboard { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var data = await _experimentService.GetDashboardDataAsync(id);
            if (data.Experiment == null)
            {
                return NotFound();
            }

            Dashboard = new ExperimentDashboardViewModel
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

            return Page();
        }

        public async Task<IActionResult> OnPostRunExperimentAsync(int id)
        {
            if (!User.IsInRole("Admin") && !User.IsInRole("Lecturer")) return Forbid();

            var (success, message) = await _experimentService.RunExperimentAsync(id);

            if (!success)
            {
                TempData["Error"] = message;
                return RedirectToPage("/Experiment/Dashboard", new { id });
            }

            TempData["Success"] = message;
            return RedirectToPage("/Experiment/Dashboard", new { id });
        }
    }
}
