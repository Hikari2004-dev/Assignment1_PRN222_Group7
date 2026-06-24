using Assignment1_PRN222_Group7_BLL.Services;
using Assignment1_PRN222_Group7_DAL.Entities;
using Assignment1_PRN222_Group7.Models.ExperimentViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7.Pages.Experiment
{
    [Authorize(Policy = "LecturerUp")]
    public class AddTestQuestionModel : PageModel
    {
        private readonly IExperimentService _experimentService;

        public AddTestQuestionModel(IExperimentService experimentService)
        {
            _experimentService = experimentService;
        }

        public string ExperimentName { get; set; } = string.Empty;

        [BindProperty]
        public TestQuestionFormViewModel TestQuestionForm { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(int experimentId)
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

            ExperimentName = experiment.Name;
            TestQuestionForm = new TestQuestionFormViewModel
            {
                ExperimentId = experimentId,
                OrderIndex = maxOrder + 1
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int experimentId)
        {
            if (string.IsNullOrWhiteSpace(TestQuestionForm.Question))
            {
                ModelState.AddModelError("TestQuestionForm.Question", "Question text is required.");
            }

            if (string.IsNullOrWhiteSpace(TestQuestionForm.GroundTruth))
            {
                ModelState.AddModelError("TestQuestionForm.GroundTruth", "Ground truth answer is required.");
            }

            if (!ModelState.IsValid)
            {
                var experiment = await _experimentService.GetExperimentByIdAsync(experimentId);
                if (experiment != null)
                {
                    ExperimentName = experiment.Name;
                }
                return Page();
            }

            var question = new TestQuestion
            {
                ExperimentId = experimentId,
                Question = TestQuestionForm.Question.Trim(),
                GroundTruth = TestQuestionForm.GroundTruth.Trim(),
                ReferenceContext = TestQuestionForm.ReferenceContext?.Trim(),
                OrderIndex = TestQuestionForm.OrderIndex
            };

            var success = await _experimentService.AddTestQuestionAsync(question);
            if (!success)
            {
                TempData["Error"] = "Failed to add test question.";
                return RedirectToPage("/Experiment/Details", new { id = experimentId });
            }

            TempData["Success"] = "Test question added successfully.";
            return RedirectToPage("/Experiment/Details", new { id = experimentId });
        }
    }
}
