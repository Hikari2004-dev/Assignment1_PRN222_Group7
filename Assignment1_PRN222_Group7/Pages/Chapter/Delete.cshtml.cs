using Assignment1_PRN222_Group7_BLL.Services;
using Assignment1_PRN222_Group7_DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7.Pages.Chapter
{
    [Authorize(Policy = "AdminOnly")]
    public class DeleteModel : PageModel
    {
        private readonly IChapterService _chapterService;
        private readonly ISubjectService _subjectService;
        private readonly IAccountService _accountService;

        public DeleteModel(IChapterService chapterService, ISubjectService subjectService, IAccountService accountService)
        {
            _chapterService = chapterService;
            _subjectService = subjectService;
            _accountService = accountService;
        }

        public Assignment1_PRN222_Group7_DAL.Entities.Subject Subject { get; set; } = null!;
        public Assignment1_PRN222_Group7_DAL.Entities.Chapter Chapter { get; set; } = null!;

        private int GetUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private async Task<bool> HasSubjectAccessAsync(int subjectId)
        {
            if (User.IsInRole("Admin")) return true;
            if (User.IsInRole("Lecturer"))
            {
                var userId = GetUserId();
                return await _accountService.IsLecturerAssignedToSubjectAsync(userId, subjectId);
            }
            return true;
        }

        public async Task<IActionResult> OnGetAsync(int subjectId, int id)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (!await HasSubjectAccessAsync(subjectId)) return Forbid();

            var subject = await _subjectService.GetSubjectByIdAsync(subjectId);
            if (subject == null) return NotFound();

            var chapter = await _chapterService.GetChapterByIdAsync(id);
            if (chapter == null || chapter.SubjectId != subjectId) return NotFound();

            Subject = subject;
            Chapter = chapter;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int subjectId, int id)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (!await HasSubjectAccessAsync(subjectId)) return Forbid();

            var subject = await _subjectService.GetSubjectByIdAsync(subjectId);
            if (subject == null) return NotFound();

            var chapter = await _chapterService.GetChapterByIdAsync(id);
            if (chapter == null || chapter.SubjectId != subjectId) return NotFound();

            await _chapterService.DeleteChapterAsync(id);
            TempData["Success"] = "Đã xóa chương.";

            return RedirectToPage("/Chapter/Index", new { subjectId });
        }
    }
}
