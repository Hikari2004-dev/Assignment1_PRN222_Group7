using Assignment1_PRN222_Group7_BLL.Services;
using Assignment1_PRN222_Group7_DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7.Pages.Document
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly IDocumentService _documentService;
        private readonly ISubjectService _subjectService;
        private readonly IAccountService _accountService;
        private readonly IWebHostEnvironment _env;

        public DetailsModel(
            IDocumentService documentService,
            ISubjectService subjectService,
            IAccountService accountService,
            IWebHostEnvironment env)
        {
            _documentService = documentService;
            _subjectService = subjectService;
            _accountService = accountService;
            _env = env;
        }

        public Assignment1_PRN222_Group7_DAL.Entities.Subject Subject { get; set; } = null!;
        public Assignment1_PRN222_Group7_DAL.Entities.Document Document { get; set; } = null!;
        public IEnumerable<DocumentChunk> Chunks { get; set; } = null!;
        public string? TxtContent { get; set; }
        public bool IsAssignedLecturer { get; set; }

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

            var doc = await _documentService.GetDocumentByIdAsync(id);
            if (doc == null || doc.SubjectId != subjectId) return NotFound();

            Subject = subject;
            Document = doc;

            Chunks = await _documentService.GetDocumentChunksAsync(id);

            IsAssignedLecturer = User.IsInRole("Lecturer") &&
                                 Subject.LecturerId.HasValue &&
                                 Subject.LecturerId.Value == GetUserId();

            // Load text content directly if TXT
            if (doc.FileType == Assignment1_PRN222_Group7_DAL.Enums.FileType.TXT)
            {
                var fullPath = Path.Combine(_env.WebRootPath, doc.FilePath);
                if (System.IO.File.Exists(fullPath))
                {
                    TxtContent = await System.IO.File.ReadAllTextAsync(fullPath);
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnGetInlineFileAsync(int subjectId, int id)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (!await HasSubjectAccessAsync(subjectId)) return Forbid();

            var doc = await _documentService.GetDocumentByIdAsync(id);
            if (doc == null || doc.SubjectId != subjectId) return NotFound();

            var fullPath = Path.Combine(_env.WebRootPath, doc.FilePath);
            if (!System.IO.File.Exists(fullPath))
            {
                return NotFound("File not found on server.");
            }

            var contentType = doc.FileType switch
            {
                Assignment1_PRN222_Group7_DAL.Enums.FileType.PDF => "application/pdf",
                Assignment1_PRN222_Group7_DAL.Enums.FileType.DOCX => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                Assignment1_PRN222_Group7_DAL.Enums.FileType.PPTX => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                Assignment1_PRN222_Group7_DAL.Enums.FileType.TXT => "text/plain",
                _ => "application/octet-stream"
            };

            return File(System.IO.File.OpenRead(fullPath), contentType);
        }
    }
}
