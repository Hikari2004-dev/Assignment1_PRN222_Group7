using Assignment1_PRN222_Group7_BLL.Services;
using Assignment1_PRN222_Group7_DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7.Pages.Chapter
{
    [Authorize(Policy = "LecturerUp")]
    public class CreateModel : PageModel
    {
        private readonly IChapterService _chapterService;
        private readonly ISubjectService _subjectService;
        private readonly IAccountService _accountService;

        public CreateModel(IChapterService chapterService, ISubjectService subjectService, IAccountService accountService)
        {
            _chapterService = chapterService;
            _subjectService = subjectService;
            _accountService = accountService;
        }

        public Assignment1_PRN222_Group7_DAL.Entities.Subject Subject { get; set; } = null!;

        [BindProperty]
        [Required(ErrorMessage = "Tiêu đề chương không được để trống.")]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [BindProperty]
        public string? Description { get; set; }

        [BindProperty]
        public int OrderIndex { get; set; } = 1;

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

        public async Task<IActionResult> OnGetAsync(int subjectId)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (!await HasSubjectAccessAsync(subjectId)) return Forbid();

            var subject = await _subjectService.GetSubjectByIdAsync(subjectId);
            if (subject == null) return NotFound();

            Subject = subject;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int subjectId)
        {
            if (!await HasSubjectAccessAsync(subjectId)) return Forbid();

            var subject = await _subjectService.GetSubjectByIdAsync(subjectId);
            if (subject == null) return NotFound();

            Subject = subject;

            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (string.IsNullOrWhiteSpace(Title))
            {
                ModelState.AddModelError("Title", "Tiêu đề chương không được để trống.");
                return Page();
            }

            var chapter = new Assignment1_PRN222_Group7_DAL.Entities.Chapter
            {
                SubjectId   = subjectId,
                Title       = Title.Trim(),
                Description = Description?.Trim(),
                OrderIndex  = OrderIndex,
                CreatedAt   = DateTime.UtcNow
            };

            await _chapterService.CreateChapterAsync(chapter);
            TempData["Success"] = $"Đã tạo chương \"{chapter.Title}\".";
            return RedirectToPage("/Chapter/Index", new { subjectId });
        }
    }
}
