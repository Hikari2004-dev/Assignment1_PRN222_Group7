using Assignment1_PRN222_Group7_BLL.Services;
using Assignment1_PRN222_Group7_DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7.Controllers
{
    /// <summary>
    /// Quản lý Chapter, luôn đặt trong ngữ cảnh một Subject cụ thể.
    /// Base route: /Subject/{subjectId}/Chapter
    /// </summary>
    [Authorize]
    [Route("Subject/{subjectId:int}/Chapter")]
    public class ChapterController : Controller
    {
        private readonly IChapterService _chapterService;
        private readonly ISubjectService _subjectService;

        public ChapterController(IChapterService chapterService, ISubjectService subjectService)
        {
            _chapterService = chapterService;
            _subjectService = subjectService;
        }

        // ─── Helper: lấy subject và trả 404 nếu không tồn tại ───────────────
        private async Task<Subject?> GetSubjectOrNullAsync(int subjectId)
            => await _subjectService.GetSubjectByIdAsync(subjectId);

        // GET /Subject/5/Chapter
        [HttpGet("")]
        public async Task<IActionResult> Index(int subjectId)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var subject = await GetSubjectOrNullAsync(subjectId);
            if (subject == null) return NotFound();

            var chapters = await _chapterService.GetChaptersBySubjectAsync(subjectId);
            ViewBag.Subject = subject;
            return View(chapters);
        }

        // GET /Subject/5/Chapter/Create
        [HttpGet("Create")]
        [Authorize(Policy = "LecturerUp")]
        public async Task<IActionResult> Create(int subjectId)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var subject = await GetSubjectOrNullAsync(subjectId);
            if (subject == null) return NotFound();
            ViewBag.Subject = subject;
            return View();
        }

        // POST /Subject/5/Chapter/Create
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "LecturerUp")]
        public async Task<IActionResult> Create(int subjectId, string title,
                                                string? description, int orderIndex)
        {
            var subject = await GetSubjectOrNullAsync(subjectId);
            if (subject == null) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.Subject = subject;
                return View();
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                ModelState.AddModelError("title", "Tiêu đề chương không được để trống.");
                ViewBag.Subject = subject;
                return View();
            }

            var chapter = new Chapter
            {
                SubjectId   = subjectId,
                Title       = title.Trim(),
                Description = description?.Trim(),
                OrderIndex  = orderIndex,
                CreatedAt   = DateTime.UtcNow
            };

            await _chapterService.CreateChapterAsync(chapter);
            TempData["Success"] = $"Đã tạo chương \"{chapter.Title}\".";
            return RedirectToAction(nameof(Index), new { subjectId });
        }

        // GET /Subject/5/Chapter/Edit/3
        [HttpGet("Edit/{id:int}")]
        [Authorize(Policy = "LecturerUp")]
        public async Task<IActionResult> Edit(int subjectId, int id)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var subject = await GetSubjectOrNullAsync(subjectId);
            if (subject == null) return NotFound();

            var chapter = await _chapterService.GetChapterByIdAsync(id);
            if (chapter == null || chapter.SubjectId != subjectId) return NotFound();

            ViewBag.Subject = subject;
            ViewData["ActionType"] = "Edit";
            return View(chapter);
        }

        // POST /Subject/5/Chapter/Edit/3
        [HttpPost("Edit/{id:int}")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "LecturerUp")]
        public async Task<IActionResult> Edit(int subjectId, int id,
                                              string title, string? description, int orderIndex)
        {
            var subject = await GetSubjectOrNullAsync(subjectId);
            if (subject == null) return NotFound();

            var chapter = await _chapterService.GetChapterByIdAsync(id);
            if (chapter == null || chapter.SubjectId != subjectId) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.Subject = subject;
                return View(chapter);
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                ModelState.AddModelError("title", "Tiêu đề chương không được để trống.");
                ViewBag.Subject = subject;
                return View(chapter);
            }

            chapter.Title       = title.Trim();
            chapter.Description = description?.Trim();
            chapter.OrderIndex  = orderIndex;

            await _chapterService.UpdateChapterAsync(chapter);
            TempData["Success"] = $"Đã cập nhật chương \"{chapter.Title}\".";
            return RedirectToAction(nameof(Index), new { subjectId });
        }

        // GET /Subject/5/Chapter/Delete/3
        [HttpGet("Delete/{id:int}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int subjectId, int id)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var subject = await GetSubjectOrNullAsync(subjectId);
            if (subject == null) return NotFound();

            var chapter = await _chapterService.GetChapterByIdAsync(id);
            if (chapter == null || chapter.SubjectId != subjectId) return NotFound();

            ViewBag.Subject = subject;
            ViewData["ActionType"] = "Delete";
            return View(chapter);
        }

        // POST /Subject/5/Chapter/Delete/3
        [HttpPost("Delete/{id:int}"), ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteConfirmed(int subjectId, int id)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            await _chapterService.DeleteChapterAsync(id);
            TempData["Success"] = "Đã xóa chương.";
            return RedirectToAction(nameof(Index), new { subjectId });
        }
    }
}
