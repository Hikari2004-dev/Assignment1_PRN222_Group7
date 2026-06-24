using Assignment1_PRN222_Group7_BLL.Services;
using Assignment1_PRN222_Group7_DAL.Entities;
using Assignment1_PRN222_Group7_DAL.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7.Pages.Experiment
{
    [Authorize(Policy = "LecturerUp")]
    public class EditModel : PageModel
    {
        private readonly IExperimentService _experimentService;
        private readonly IUnitOfWork _unitOfWork;

        public EditModel(IExperimentService experimentService, IUnitOfWork unitOfWork)
        {
            _experimentService = experimentService;
            _unitOfWork = unitOfWork;
        }

        public List<Assignment1_PRN222_Group7_DAL.Entities.Subject> AvailableSubjects { get; set; } = new();

        [BindProperty]
        public int Id { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Experiment name is required.")]
        public string Name { get; set; } = string.Empty;

        [BindProperty]
        public string? Description { get; set; }

        [BindProperty]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a subject.")]
        public int SubjectId { get; set; }

        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var experiment = await _experimentService.GetExperimentByIdAsync(id);
            if (experiment == null)
            {
                return NotFound();
            }

            var subjectRepo = _unitOfWork.GetRepository<Assignment1_PRN222_Group7_DAL.Entities.Subject>();
            var subjects = await subjectRepo.FindAsync(s => s.IsActive);
            AvailableSubjects = subjects.ToList();

            Id = experiment.Id;
            Name = experiment.Name;
            Description = experiment.Description;
            SubjectId = experiment.SubjectId;
            Status = experiment.Status.ToString();
            CreatedAt = experiment.CreatedAt;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var experiment = await _experimentService.GetExperimentByIdAsync(Id);
            if (experiment == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                var subjectRepo = _unitOfWork.GetRepository<Assignment1_PRN222_Group7_DAL.Entities.Subject>();
                var subjects = await subjectRepo.FindAsync(s => s.IsActive);
                AvailableSubjects = subjects.ToList();
                Status = experiment.Status.ToString();
                CreatedAt = experiment.CreatedAt;
                return Page();
            }

            experiment.Name = Name.Trim();
            experiment.Description = Description?.Trim();
            experiment.SubjectId = SubjectId;

            var success = await _experimentService.UpdateExperimentAsync(experiment);
            if (!success)
            {
                ModelState.AddModelError("", "Failed to update experiment.");
                var subjectRepo = _unitOfWork.GetRepository<Assignment1_PRN222_Group7_DAL.Entities.Subject>();
                var subjects = await subjectRepo.FindAsync(s => s.IsActive);
                AvailableSubjects = subjects.ToList();
                Status = experiment.Status.ToString();
                CreatedAt = experiment.CreatedAt;
                return Page();
            }

            TempData["Success"] = $"Experiment \"{experiment.Name}\" updated successfully.";
            return RedirectToPage("/Experiment/Details", new { id = experiment.Id });
        }
    }
}
