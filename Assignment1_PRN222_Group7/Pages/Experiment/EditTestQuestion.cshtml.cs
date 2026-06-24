using Assignment1_PRN222_Group7_BLL.Services;
using Assignment1_PRN222_Group7_DAL.Entities;
using Assignment1_PRN222_Group7.Models.ExperimentViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7.Pages.Experiment
{
    [Authorize(Policy = "LecturerUp")]
    public class EditTestQuestionModel : PageModel
    {
        private readonly IExperimentService _experimentService;

        public EditTestQuestionModel(IExperimentService experimentService)
        {
            _experimentService = experimentService;
        }

        [BindProperty]
        public TestQuestionFormViewModel TestQuestionForm { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var question = await _experimentService.GetTestQuestionByIdAsync(id);
            if (question == null)
            {
                return NotFound();
            }

            TestQuestionForm = new TestQuestionFormViewModel
            {
                Id = question.Id,
                ExperimentId = question.ExperimentId,
                Question = question.Question,
                GroundTruth = question.GroundTruth,
                ReferenceContext = question.ReferenceContext,
                OrderIndex = question.OrderIndex
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var question = await _experimentService.GetTestQuestionByIdAsync(TestQuestionForm.Id);
            if (question == null)
            {
                return NotFound();
            }

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
                return Page();
            }

            question.Question = TestQuestionForm.Question.Trim();
            question.GroundTruth = TestQuestionForm.GroundTruth.Trim();
            question.ReferenceContext = TestQuestionForm.ReferenceContext?.Trim();
            question.OrderIndex = TestQuestionForm.OrderIndex;

            var success = await _experimentService.UpdateTestQuestionAsync(question);
            if (!success)
            {
                TempData["Error"] = "Failed to update test question.";
                return RedirectToPage("/Experiment/Details", new { id = TestQuestionForm.ExperimentId });
            }

            TempData["Success"] = "Test question updated successfully.";
            return RedirectToPage("/Experiment/Details", new { id = TestQuestionForm.ExperimentId });
        }
    }
}
