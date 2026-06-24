using Assignment1_PRN222_Group7_BLL.Services;
using Assignment1_PRN222_Group7_DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7.Pages.Document
{
    [Authorize(Roles = "Lecturer")]
    public class UploadModel : PageModel
    {
        private readonly IDocumentService _documentService;
        private readonly ISubjectService _subjectService;
        private readonly IChapterService _chapterService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IWebHostEnvironment _env;
        private readonly IAccountService _accountService;
        private readonly ISubscriptionService _subscriptionService;

        public UploadModel(
            IDocumentService documentService,
            ISubjectService subjectService,
            IChapterService chapterService,
            IServiceScopeFactory scopeFactory,
            IWebHostEnvironment env,
            IAccountService accountService,
            ISubscriptionService subscriptionService)
        {
            _documentService = documentService;
            _subjectService = subjectService;
            _chapterService = chapterService;
            _scopeFactory = scopeFactory;
            _env = env;
            _accountService = accountService;
            _subscriptionService = subscriptionService;
        }

        public Assignment1_PRN222_Group7_DAL.Entities.Subject Subject { get; set; } = null!;
        public IEnumerable<Assignment1_PRN222_Group7_DAL.Entities.Chapter> Chapters { get; set; } = null!;

        [BindProperty]
        [Required(ErrorMessage = "Tiêu đề không được để trống.")]
        public string Title { get; set; } = string.Empty;

        [BindProperty]
        public int? ChapterId { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Vui lòng chọn file để upload.")]
        public IFormFile UploadedFile { get; set; } = null!;

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

        public async Task<IActionResult> OnGetAsync(int subjectId)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (!await HasSubjectAccessAsync(subjectId)) return Forbid();

            var subject = await _subjectService.GetSubjectByIdAsync(subjectId);
            if (subject == null) return NotFound();

            Subject = subject;
            Chapters = await _chapterService.GetChaptersBySubjectAsync(subjectId);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int subjectId)
        {
            if (!await HasSubjectAccessAsync(subjectId)) return Forbid();

            var subject = await _subjectService.GetSubjectByIdAsync(subjectId);
            if (subject == null) return NotFound();

            Subject = subject;
            Chapters = await _chapterService.GetChaptersBySubjectAsync(subjectId);

            // Check subscription upload limit
            var userId = GetUserId();
            var (canUpload, uploadLimitMsg) = await _subscriptionService.CheckDocumentUploadLimitAsync(userId);
            if (!canUpload)
            {
                ModelState.AddModelError("UploadedFile", uploadLimitMsg ?? "Upload limit reached.");
                return Page();
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (UploadedFile == null || UploadedFile.Length == 0)
            {
                ModelState.AddModelError("UploadedFile", "Vui lòng chọn file để upload.");
                return Page();
            }

            if (string.IsNullOrWhiteSpace(Title))
            {
                ModelState.AddModelError("Title", "Tiêu đề không được để trống.");
                return Page();
            }

            // Validate extension
            var allowedExts = new[] { ".pdf", ".docx", ".pptx", ".txt" };
            var ext = Path.GetExtension(UploadedFile.FileName).ToLowerInvariant();
            if (!allowedExts.Contains(ext))
            {
                ModelState.AddModelError("UploadedFile", "Chỉ hỗ trợ file PDF, DOCX, PPTX, TXT.");
                return Page();
            }

            using var stream = UploadedFile.OpenReadStream();
            var uploadModel = new DocumentUploadModel(
                stream,
                UploadedFile.FileName,
                UploadedFile.Length,
                subjectId,
                ChapterId,
                Title,
                userId,
                _env.WebRootPath
            );
            var doc = await _documentService.UploadDocumentAsync(uploadModel);

            // Fire background indexing (non-blocking)
            var docId = doc.Id;
            var webRoot = _env.WebRootPath;
            _ = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var indexer = scope.ServiceProvider.GetRequiredService<IDocumentIndexingService>();
                await indexer.IndexDocumentAsync(docId, webRoot);
            });

            TempData["Success"] = $"Đã upload tài liệu \"{doc.Title}\". Hệ thống đang xử lý indexing...";
            return RedirectToPage("/Document/Index", new { subjectId });
        }
    }
}
