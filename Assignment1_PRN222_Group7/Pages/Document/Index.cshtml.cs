using Assignment1_PRN222_Group7_BLL.Services;
using Assignment1_PRN222_Group7_DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7.Pages.Document
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly IDocumentService _documentService;
        private readonly ISubjectService _subjectService;
        private readonly IChapterService _chapterService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IWebHostEnvironment _env;
        private readonly IAccountService _accountService;

        public IndexModel(
            IDocumentService documentService,
            ISubjectService subjectService,
            IChapterService chapterService,
            IServiceScopeFactory scopeFactory,
            IWebHostEnvironment env,
            IAccountService accountService)
        {
            _documentService = documentService;
            _subjectService = subjectService;
            _chapterService = chapterService;
            _scopeFactory = scopeFactory;
            _env = env;
            _accountService = accountService;
        }

        public Assignment1_PRN222_Group7_DAL.Entities.Subject Subject { get; set; } = null!;
        public IEnumerable<Assignment1_PRN222_Group7_DAL.Entities.Document> Documents { get; set; } = null!;
        public int? FilterChapterId { get; set; }
        public Assignment1_PRN222_Group7_DAL.Entities.Chapter? FilterChapter { get; set; }
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

        public async Task<IActionResult> OnGetAsync(int subjectId, int? chapterId)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (!await HasSubjectAccessAsync(subjectId)) return Forbid();

            var subject = await _subjectService.GetSubjectByIdAsync(subjectId);
            if (subject == null) return NotFound();

            Subject = subject;
            FilterChapterId = chapterId;
            Documents = await _documentService.GetDocumentsBySubjectAsync(subjectId, chapterId);

            if (chapterId.HasValue)
            {
                FilterChapter = await _chapterService.GetChapterByIdAsync(chapterId.Value);
            }

            IsAssignedLecturer = User.IsInRole("Lecturer") &&
                                 Subject.LecturerId.HasValue &&
                                 Subject.LecturerId.Value == GetUserId();

            return Page();
        }

        public async Task<IActionResult> OnPostReIndexAsync(int subjectId, int id)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (!User.IsInRole("Lecturer")) return Forbid();
            var userId = GetUserId();
            var isAssigned = await _accountService.IsLecturerAssignedToSubjectAsync(userId, subjectId);
            if (!isAssigned) return Forbid();

            var doc = await _documentService.GetDocumentByIdAsync(id);
            if (doc == null || doc.SubjectId != subjectId) return NotFound();

            var webRoot = _env.WebRootPath;
            _ = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var indexer = scope.ServiceProvider.GetRequiredService<IDocumentIndexingService>();
                await indexer.IndexDocumentAsync(id, webRoot);
            });

            TempData["Success"] = $"Đang re-index tài liệu \"{doc.Title}\"...";
            return RedirectToPage("/Document/Index", new { subjectId });
        }

        public async Task<IActionResult> OnGetDownloadAsync(int subjectId, int id)
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

            return File(System.IO.File.OpenRead(fullPath), contentType, doc.OriginalFileName);
        }
    }
}
