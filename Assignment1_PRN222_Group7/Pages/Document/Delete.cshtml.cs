using Assignment1_PRN222_Group7_BLL.Services;
using Assignment1_PRN222_Group7_DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7.Pages.Document
{
    [Authorize(Roles = "Lecturer")]
    public class DeleteModel : PageModel
    {
        private readonly IDocumentService _documentService;
        private readonly ISubjectService _subjectService;
        private readonly IWebHostEnvironment _env;
        private readonly IAccountService _accountService;

        public DeleteModel(
            IDocumentService documentService,
            ISubjectService subjectService,
            IWebHostEnvironment env,
            IAccountService accountService)
        {
            _documentService = documentService;
            _subjectService = subjectService;
            _env = env;
            _accountService = accountService;
        }

        public Assignment1_PRN222_Group7_DAL.Entities.Subject Subject { get; set; } = null!;
        public Assignment1_PRN222_Group7_DAL.Entities.Document Document { get; set; } = null!;

        private int GetUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private async Task<bool> HasSubjectAccessAsync(int subjectId)
        {
            if (User.IsInRole("Lecturer"))
            {
                var userId = GetUserId();
                return await _accountService.IsLecturerAssignedToSubjectAsync(userId, subjectId);
            }
            return false;
        }

        public async Task<IActionResult> OnGetAsync(int subjectId, int id)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (!await HasSubjectAccessAsync(subjectId)) return Forbid();

            var subject = await _subjectService.GetSubjectByIdAsync(subjectId);
            if (subject == null) return NotFound();

            var doc = await _documentService.GetDocumentByIdAsync(id);
            if (doc == null || doc.SubjectId != subjectId) return NotFound();

            Subject = subject;
            Document = doc;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int subjectId, int id)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (!await HasSubjectAccessAsync(subjectId)) return Forbid();

            await _documentService.DeleteDocumentAsync(id, _env.WebRootPath);
            TempData["Success"] = "Đã xóa tài liệu.";
            return RedirectToPage("/Document/Index", new { subjectId });
        }
    }
}
