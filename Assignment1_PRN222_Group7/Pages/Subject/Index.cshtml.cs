using Assignment1_PRN222_Group7_BLL.Services;
using Assignment1_PRN222_Group7_DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7.Pages.Subject
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ISubjectService _subjectService;
        private readonly IAccountService _accountService;

        public IndexModel(ISubjectService subjectService, IAccountService accountService)
        {
            _subjectService = subjectService;
            _accountService = accountService;
        }

        public List<Assignment1_PRN222_Group7_DAL.Entities.Subject> Subjects { get; set; } = [];

        public async Task<IActionResult> OnGetAsync()
        {
            var all = await _subjectService.GetAllSubjectsAsync();
            var subjects = all.ToList();

            if (User.IsInRole("Lecturer"))
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var assigned = await _accountService.GetSubjectsAssignedToLecturerAsync(userId);
                var assignedIds = assigned.Select(s => s.Id).ToHashSet();
                subjects = subjects.Where(s => assignedIds.Contains(s.Id)).ToList();
            }

            Subjects = subjects;
            return Page();
        }
    }
}
