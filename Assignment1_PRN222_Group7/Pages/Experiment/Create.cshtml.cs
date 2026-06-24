using Assignment1_PRN222_Group7_BLL.Services;
using Assignment1_PRN222_Group7_DAL.Entities;
using Assignment1_PRN222_Group7_DAL.Enums;
using Assignment1_PRN222_Group7_DAL.Repositories;
using Assignment1_PRN222_Group7.Models.ExperimentViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7.Pages.Experiment
{
    [Authorize(Policy = "LecturerUp")]
    public class CreateModel : PageModel
    {
        private readonly IExperimentService _experimentService;
        private readonly IUnitOfWork _unitOfWork;

        public CreateModel(IExperimentService experimentService, IUnitOfWork unitOfWork)
        {
            _experimentService = experimentService;
            _unitOfWork = unitOfWork;
        }

        public List<Assignment1_PRN222_Group7_DAL.Entities.Subject> AvailableSubjects { get; set; } = new();

        [BindProperty]
        [Required(ErrorMessage = "Experiment name is required.")]
        public string Name { get; set; } = string.Empty;

        [BindProperty]
        public string? Description { get; set; }

        [BindProperty]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a subject.")]
        public int SubjectId { get; set; }

        private int GetUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        public async Task<IActionResult> OnGetAsync()
        {
            var subjectRepo = _unitOfWork.GetRepository<Assignment1_PRN222_Group7_DAL.Entities.Subject>();
            var subjects = await subjectRepo.FindAsync(s => s.IsActive);
            AvailableSubjects = subjects.ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                var subjectRepo = _unitOfWork.GetRepository<Assignment1_PRN222_Group7_DAL.Entities.Subject>();
                var subjects = await subjectRepo.FindAsync(s => s.IsActive);
                AvailableSubjects = subjects.ToList();
                return Page();
            }

            var experiment = new Assignment1_PRN222_Group7_DAL.Entities.Experiment
            {
                Name = Name.Trim(),
                Description = Description?.Trim(),
                SubjectId = SubjectId,
                CreatedBy = GetUserId(),
                Status = ExperimentStatus.Draft
            };

            var success = await _experimentService.CreateExperimentAsync(experiment);
            if (!success)
            {
                ModelState.AddModelError("", "Failed to create experiment.");
                var subjectRepo = _unitOfWork.GetRepository<Assignment1_PRN222_Group7_DAL.Entities.Subject>();
                var subjects = await subjectRepo.FindAsync(s => s.IsActive);
                AvailableSubjects = subjects.ToList();
                return Page();
            }

            TempData["Success"] = $"Experiment \"{experiment.Name}\" created successfully.";
            return RedirectToPage("/Experiment/Details", new { id = experiment.Id });
        }
    }
}
