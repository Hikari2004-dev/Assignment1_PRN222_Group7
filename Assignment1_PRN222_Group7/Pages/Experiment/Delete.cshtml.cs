using Assignment1_PRN222_Group7_BLL.Services;
using Assignment1_PRN222_Group7_DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7.Pages.Experiment
{
    [Authorize(Policy = "AdminOnly")]
    public class DeleteModel : PageModel
    {
        private readonly IExperimentService _experimentService;

        public DeleteModel(IExperimentService experimentService)
        {
            _experimentService = experimentService;
        }

        public Assignment1_PRN222_Group7_DAL.Entities.Experiment Experiment { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var experiment = await _experimentService.GetExperimentByIdAsync(id);
            if (experiment == null)
            {
                return NotFound();
            }

            Experiment = experiment;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var experiment = await _experimentService.GetExperimentByIdAsync(id);
            var name = experiment?.Name ?? "Experiment";

            var success = await _experimentService.DeleteExperimentAsync(id);
            if (!success)
            {
                TempData["Error"] = "Failed to delete experiment.";
                return RedirectToPage("/Experiment/Index");
            }

            TempData["Success"] = $"Experiment \"{name}\" deleted successfully.";
            return RedirectToPage("/Experiment/Index");
        }
    }
}
