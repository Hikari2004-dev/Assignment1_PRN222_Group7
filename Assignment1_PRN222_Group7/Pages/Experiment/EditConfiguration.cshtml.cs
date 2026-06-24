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
    public class EditConfigurationModel : PageModel
    {
        private readonly IExperimentService _experimentService;

        public EditConfigurationModel(IExperimentService experimentService)
        {
            _experimentService = experimentService;
        }

        [BindProperty]
        public ConfigurationFormViewModel ConfigurationForm { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var config = await _experimentService.GetConfigurationByIdAsync(id);
            if (config == null)
            {
                return NotFound();
            }

            ConfigurationForm = new ConfigurationFormViewModel
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

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var config = await _experimentService.GetConfigurationByIdAsync(ConfigurationForm.Id);
            if (config == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(ConfigurationForm.ConfigName))
            {
                ModelState.AddModelError("ConfigurationForm.ConfigName", "Configuration name is required.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            config.ConfigName = ConfigurationForm.ConfigName.Trim();
            config.ChunkingStrategy = Enum.Parse<ChunkingStrategy>(ConfigurationForm.ChunkingStrategy);
            config.ChunkSize = ConfigurationForm.ChunkSize;
            config.ChunkOverlap = ConfigurationForm.ChunkOverlap;
            config.EmbeddingModel = Enum.Parse<EmbeddingModel>(ConfigurationForm.EmbeddingModel);
            config.RetrievalMethod = Enum.Parse<RetrievalMethod>(ConfigurationForm.RetrievalMethod);
            config.TopK = ConfigurationForm.TopK;
            config.SimilarityThreshold = ConfigurationForm.SimilarityThreshold;

            var success = await _experimentService.UpdateConfigurationAsync(config);
            if (!success)
            {
                TempData["Error"] = "Failed to update configuration.";
                return RedirectToPage("/Experiment/Details", new { id = ConfigurationForm.ExperimentId });
            }

            TempData["Success"] = $"Configuration \"{config.ConfigName}\" updated successfully.";
            return RedirectToPage("/Experiment/Details", new { id = ConfigurationForm.ExperimentId });
        }
    }
}
