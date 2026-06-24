using Assignment1_PRN222_Group7_BLL.Services;
using Assignment1_PRN222_Group7_DAL.Entities;
using Assignment1_PRN222_Group7_DAL.Enums;
using Assignment1_PRN222_Group7.Models.ExperimentViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7.Pages.Experiment
{
    [Authorize(Policy = "LecturerUp")]
    public class AddConfigurationModel : PageModel
    {
        private readonly IExperimentService _experimentService;

        public AddConfigurationModel(IExperimentService experimentService)
        {
            _experimentService = experimentService;
        }

        public string ExperimentName { get; set; } = string.Empty;

        [BindProperty]
        public ConfigurationFormViewModel ConfigurationForm { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(int experimentId)
        {
            var experiment = await _experimentService.GetExperimentByIdAsync(experimentId);
            if (experiment == null)
            {
                return NotFound();
            }

            ExperimentName = experiment.Name;
            ConfigurationForm = new ConfigurationFormViewModel
            {
                ExperimentId = experimentId
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int experimentId)
        {
            if (string.IsNullOrWhiteSpace(ConfigurationForm.ConfigName))
            {
                ModelState.AddModelError("ConfigurationForm.ConfigName", "Configuration name is required.");
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

            var config = new ExperimentConfiguration
            {
                ExperimentId = experimentId,
                ConfigName = ConfigurationForm.ConfigName.Trim(),
                ChunkingStrategy = Enum.Parse<ChunkingStrategy>(ConfigurationForm.ChunkingStrategy),
                ChunkSize = ConfigurationForm.ChunkSize,
                ChunkOverlap = ConfigurationForm.ChunkOverlap,
                EmbeddingModel = Enum.Parse<EmbeddingModel>(ConfigurationForm.EmbeddingModel),
                RetrievalMethod = Enum.Parse<RetrievalMethod>(ConfigurationForm.RetrievalMethod),
                TopK = ConfigurationForm.TopK,
                SimilarityThreshold = ConfigurationForm.SimilarityThreshold
            };

            var success = await _experimentService.AddConfigurationAsync(config);
            if (!success)
            {
                TempData["Error"] = "Failed to add configuration.";
                return RedirectToPage("/Experiment/Details", new { id = experimentId });
            }

            TempData["Success"] = $"Configuration \"{config.ConfigName}\" added successfully.";
            return RedirectToPage("/Experiment/Details", new { id = experimentId });
        }
    }
}
