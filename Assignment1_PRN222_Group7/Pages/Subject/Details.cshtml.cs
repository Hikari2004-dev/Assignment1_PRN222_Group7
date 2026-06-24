using Assignment1_PRN222_Group7_BLL.Services;
using Assignment1_PRN222_Group7_DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7.Pages.Subject
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly ISubjectService _subjectService;
        private readonly IAccountService _accountService;

        public DetailsModel(ISubjectService subjectService, IAccountService accountService)
        {
            _subjectService = subjectService;
            _accountService = accountService;
        }

        public Assignment1_PRN222_Group7_DAL.Entities.Subject Subject { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var subject = await _subjectService.GetSubjectByIdAsync(id);
            if (subject == null)
            {
                return NotFound();
            }

            if (User.IsInRole("Lecturer"))
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var isAssigned = await _accountService.IsLecturerAssignedToSubjectAsync(userId, id);
                if (!isAssigned)
                {
                    return Forbid();
                }
            }

            Subject = subject;
            return Page();
        }
    }
}
