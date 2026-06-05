using Assignment1_PRN222_Group7_BLL.Services;
using Assignment1_PRN222_Group7_DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class UserController : Controller
    {
        private readonly IAccountService _accountService;
        private readonly ISubjectService _subjectService;

        public UserController(IAccountService accountService, ISubjectService subjectService)
        {
            _accountService = accountService;
            _subjectService = subjectService;
        }

        // GET: /User
        public async Task<IActionResult> Index(string? search, string? roleFilter)
        {
            var users = await _accountService.GetAllUsersAsync();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var query = search.Trim().ToLower();
                users = users.Where(u => u.FullName.ToLower().Contains(query) || u.Email.ToLower().Contains(query)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(roleFilter))
            {
                users = users.Where(u => u.Role.Name == roleFilter).ToList();
            }

            ViewData["Search"] = search;
            ViewData["RoleFilter"] = roleFilter;
            return View(users);
        }

        // GET: /User/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _accountService.GetUserByIdAsync(id);
            if (user == null) return NotFound();

            ViewBag.Roles = await _accountService.GetAvailableRolesAsync();
            return View(user);
        }

        // POST: /User/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string fullName, string email, int roleId, string? studentOrStaffId, string? password, string? confirmPassword)
        {
            var user = await _accountService.GetUserByIdAsync(id);
            if (user == null) return NotFound();

            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError("", "Họ tên và Email không được để trống.");
                ViewBag.Roles = await _accountService.GetAvailableRolesAsync();
                return View(user);
            }

            if (!string.IsNullOrEmpty(password) && password != confirmPassword)
            {
                ModelState.AddModelError("", "Mật khẩu xác nhận không khớp.");
                ViewBag.Roles = await _accountService.GetAvailableRolesAsync();
                return View(user);
            }

            // Update details
            user.FullName = fullName.Trim();
            user.Email = email.Trim();
            user.RoleId = roleId;
            user.StudentOrStaffId = studentOrStaffId?.Trim();
            
            if (!string.IsNullOrEmpty(password))
            {
                user.PasswordHash = password; // Will be hashed in service
            }
            else
            {
                user.PasswordHash = ""; // Keep existing
            }

            var success = await _accountService.UpdateUserAsync(user);
            if (!success)
            {
                ModelState.AddModelError("", "Email này đã tồn tại ở tài khoản khác.");
                ViewBag.Roles = await _accountService.GetAvailableRolesAsync();
                return View(user);
            }

            TempData["Success"] = $"Đã cập nhật thông tin tài khoản \"{user.FullName}\" thành công.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /User/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var success = await _accountService.ToggleUserStatusAsync(id);
            if (!success)
            {
                TempData["Error"] = "Không thể thay đổi trạng thái tài khoản này.";
            }
            else
            {
                TempData["Success"] = "Đã cập nhật trạng thái hoạt động tài khoản.";
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: /User/AssignSubjects/5
        public async Task<IActionResult> AssignSubjects(int id)
        {
            var user = await _accountService.GetUserByIdAsync(id);
            if (user == null || user.Role.Name != "Lecturer")
            {
                return NotFound();
            }

            var allSubjects = await _subjectService.GetAllSubjectsAsync();
            var assigned = await _accountService.GetSubjectsAssignedToLecturerAsync(id);
            var assignedIds = assigned.Select(s => s.Id).ToHashSet();

            ViewBag.User = user;
            ViewBag.AllSubjects = allSubjects.ToList();
            ViewBag.AssignedIds = assignedIds;

            return View();
        }

        // POST: /User/AssignSubjects/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignSubjects(int id, List<int> subjectIds)
        {
            var user = await _accountService.GetUserByIdAsync(id);
            if (user == null || user.Role.Name != "Lecturer")
            {
                return NotFound();
            }

            var success = await _accountService.AssignSubjectsToLecturerAsync(id, subjectIds);
            if (!success)
            {
                TempData["Error"] = "Gặp lỗi khi phân quyền môn học.";
            }
            else
            {
                TempData["Success"] = $"Đã cập nhật phân quyền môn học cho giảng viên \"{user.FullName}\".";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
