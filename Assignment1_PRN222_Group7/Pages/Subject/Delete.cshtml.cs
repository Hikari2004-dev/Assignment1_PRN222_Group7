using Assignment1_PRN222_Group7.Hubs;
using Assignment1_PRN222_Group7_BLL.Services;
using Assignment1_PRN222_Group7_DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7.Pages.Subject
{
    [Authorize(Policy = "AdminOnly")]
    public class DeleteModel : SubjectHubPageModel
    {
        private readonly ISubjectService _subjectService;

        public DeleteModel(ISubjectService subjectService, IHubContext<SubjectHub> hubContext) 
            : base(hubContext)
        {
            _subjectService = subjectService;
        }

        [BindProperty]
        public int Id { get; set; }

        public Assignment1_PRN222_Group7_DAL.Entities.Subject Subject { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var subject = await _subjectService.GetSubjectByIdAsync(id);
            if (subject == null)
            {
                return NotFound();
            }

            Subject = subject;
            Id = subject.Id;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var subject = await _subjectService.GetSubjectByIdAsync(Id);
            if (subject == null)
            {
                return NotFound();
            }

            var name = subject.Name;
            await _subjectService.DeleteSubjectAsync(Id);

            // SignalR broadcast the deleted subject ID
            await NotifySubjectDeleted(Id);

            TempData["Success"] = $"Đã xóa môn học \"{name}\" thành công.";
            return RedirectToPage("./Index");
        }
    }
}
