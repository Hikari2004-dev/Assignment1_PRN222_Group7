using Assignment1_PRN222_Group7_BLL.Services;
using Assignment1_PRN222_Group7_DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7.Controllers
{
    [Authorize]
    public class SubjectController : Controller
    {
        private readonly ISubjectService _subjectService;

        public SubjectController(ISubjectService subjectService)
        {
            _subjectService = subjectService;
        }

        // GET /Subject
        public async Task<IActionResult> Index()
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var subjects = await _subjectService.GetAllSubjectsAsync();
            return View(subjects);
        }

        // GET /Subject/Details/5
        public async Task<IActionResult> Details(int id)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var subject = await _subjectService.GetSubjectByIdAsync(id);
            if (subject == null) return NotFound();
            return View(subject);
        }

        // GET /Subject/Create
        [Authorize(Policy = "LecturerUp")]
        public IActionResult Create()
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            return View();
        }

        // POST /Subject/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "LecturerUp")]
        public async Task<IActionResult> Create(string code, string name, string? description)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            {
                ModelState.AddModelError("", "Mã môn và tên môn không được để trống.");
                return View();
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var subject = new Subject
            {
                Code        = code.Trim().ToUpper(),
                Name        = name.Trim(),
                Description = description?.Trim(),
                CreatedBy   = userId,
                CreatedAt   = DateTime.UtcNow,
                IsActive    = true
            };

            var ok = await _subjectService.CreateSubjectAsync(subject);
            if (!ok)
            {
                ModelState.AddModelError("code", $"Mã môn \"{code}\" đã tồn tại.");
                return View();
            }

            TempData["Success"] = $"Đã tạo môn học \"{subject.Name}\" thành công.";
            return RedirectToAction(nameof(Index));
        }

        // GET /Subject/Edit/5
        [Authorize(Policy = "LecturerUp")]
        public async Task<IActionResult> Edit(int id)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var subject = await _subjectService.GetSubjectByIdAsync(id);
            if (subject == null) return NotFound();
            ViewData["ActionType"] = "Edit";
            return View(subject);
        }

        // POST /Subject/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "LecturerUp")]
        public async Task<IActionResult> Edit(int id, string code, string name,
                                              string? description, bool isActive)
        {
            var subject = await _subjectService.GetSubjectByIdAsync(id);
            if (subject == null) return NotFound();

            if (!ModelState.IsValid)
            {
                return View(subject);
            }

            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            {
                ModelState.AddModelError("", "Mã môn và tên môn không được để trống.");
                return View(subject);
            }

            subject.Code        = code.Trim().ToUpper();
            subject.Name        = name.Trim();
            subject.Description = description?.Trim();
            subject.IsActive    = isActive;

            var ok = await _subjectService.UpdateSubjectAsync(subject);
            if (!ok)
            {
                ModelState.AddModelError("code", $"Mã môn \"{code}\" đã tồn tại ở môn khác.");
                return View(subject);
            }

            TempData["Success"] = $"Đã cập nhật môn học \"{subject.Name}\".";
            return RedirectToAction(nameof(Index));
        }

        // GET /Subject/Delete/5
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var subject = await _subjectService.GetSubjectByIdAsync(id);
            if (subject == null) return NotFound();
            ViewData["ActionType"] = "Delete";
            return View(subject);
        }

        // POST /Subject/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            await _subjectService.DeleteSubjectAsync(id);
            TempData["Success"] = "Đã xóa môn học.";
            return RedirectToAction(nameof(Index));
        }
    }
}
