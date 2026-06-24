using Assignment1_PRN222_Group7_BLL.Services;
using Assignment1_PRN222_Group7_DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7.Pages.Experiment
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IExperimentService _experimentService;

        public IndexModel(IExperimentService experimentService)
        {
            _experimentService = experimentService;
        }

        public IEnumerable<Assignment1_PRN222_Group7_DAL.Entities.Experiment> Experiments { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync()
        {
            Experiments = await _experimentService.GetAllExperimentsAsync();
            return Page();
        }
    }
}
