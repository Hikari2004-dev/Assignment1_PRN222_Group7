using Assignment1_PRN222_Group7_BLL.Services;
using Assignment1_PRN222_Group7_DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Assignment1_PRN222_Group7.Controllers
{
    /// <summary>
    /// Quản lý tài liệu trong ngữ cảnh một Subject.
    /// Base route: /Subject/{subjectId}/Document
    /// </summary>
    [Authorize]
    [Route("Subject/{subjectId:int}/Document")]
    public class DocumentController : Controller
    {
        private readonly IDocumentService _documentService;
        private readonly ISubjectService _subjectService;
        private readonly IChapterService _chapterService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IWebHostEnvironment _env;

        public DocumentController(
            IDocumentService documentService,
            ISubjectService subjectService,
            IChapterService chapterService,
            IServiceScopeFactory scopeFactory,
            IWebHostEnvironment env)
        {
            _documentService = documentService;
            _subjectService  = subjectService;
            _chapterService  = chapterService;
            _scopeFactory    = scopeFactory;
            _env             = env;
        }

        private int GetUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // GET /Subject/5/Document?chapterId=3
        [HttpGet("")]
        public async Task<IActionResult> Index(int subjectId, int? chapterId)
        {
            var subject = await _subjectService.GetSubjectByIdAsync(subjectId);
            if (subject == null) return NotFound();

            var docs = await _documentService.GetDocumentsBySubjectAsync(subjectId, chapterId);
            ViewBag.Subject = subject;
            ViewBag.FilterChapterId = chapterId;

            if (chapterId.HasValue)
            {
                var chapter = await _chapterService.GetChapterByIdAsync(chapterId.Value);
                ViewBag.FilterChapter = chapter;
            }

            return View(docs);
        }

        // GET /Subject/5/Document/Upload
        [HttpGet("Upload")]
        [Authorize(Policy = "LecturerUp")]
        public async Task<IActionResult> Upload(int subjectId)
        {
            var subject = await _subjectService.GetSubjectByIdAsync(subjectId);
            if (subject == null) return NotFound();

            ViewBag.Subject = subject;
            ViewBag.Chapters = await _chapterService.GetChaptersBySubjectAsync(subjectId);
            return View();
        }

        // POST /Subject/5/Document/Upload
        [HttpPost("Upload")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "LecturerUp")]
        public async Task<IActionResult> Upload(int subjectId, string title, int? chapterId, IFormFile file)
        {
            var subject = await _subjectService.GetSubjectByIdAsync(subjectId);
            if (subject == null) return NotFound();

            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("file", "Vui lòng chọn file để upload.");
                ViewBag.Subject = subject;
                ViewBag.Chapters = await _chapterService.GetChaptersBySubjectAsync(subjectId);
                return View();
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                ModelState.AddModelError("title", "Tiêu đề không được để trống.");
                ViewBag.Subject = subject;
                ViewBag.Chapters = await _chapterService.GetChaptersBySubjectAsync(subjectId);
                return View();
            }

            // Validate extension
            var allowedExts = new[] { ".pdf", ".docx", ".pptx", ".txt" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExts.Contains(ext))
            {
                ModelState.AddModelError("file", "Chỉ hỗ trợ file PDF, DOCX, PPTX, TXT.");
                ViewBag.Subject = subject;
                ViewBag.Chapters = await _chapterService.GetChaptersBySubjectAsync(subjectId);
                return View();
            }

            var userId = GetUserId();
            using var stream = file.OpenReadStream();
            var doc = await _documentService.UploadDocumentAsync(stream, file.FileName, file.Length,
                subjectId, chapterId, title, userId, _env.WebRootPath);

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
            return RedirectToAction(nameof(Index), new { subjectId });
        }

        // POST /Subject/5/Document/Index/3
        [HttpPost("Index/{id:int}")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "LecturerUp")]
        public async Task<IActionResult> ReIndex(int subjectId, int id)
        {
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
            return RedirectToAction(nameof(Index), new { subjectId });
        }

        // GET /Subject/5/Document/Delete/3
        [HttpGet("Delete/{id:int}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int subjectId, int id)
        {
            var subject = await _subjectService.GetSubjectByIdAsync(subjectId);
            if (subject == null) return NotFound();

            var doc = await _documentService.GetDocumentByIdAsync(id);
            if (doc == null || doc.SubjectId != subjectId) return NotFound();

            ViewBag.Subject = subject;
            return View(doc);
        }

        // POST /Subject/5/Document/Delete/3
        [HttpPost("Delete/{id:int}"), ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteConfirmed(int subjectId, int id)
        {
            await _documentService.DeleteDocumentAsync(id, _env.WebRootPath);
            TempData["Success"] = "Đã xóa tài liệu.";
            return RedirectToAction(nameof(Index), new { subjectId });
        }
    }
}
