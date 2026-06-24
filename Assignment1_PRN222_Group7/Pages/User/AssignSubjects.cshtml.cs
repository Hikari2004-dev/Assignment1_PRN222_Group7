using Assignment1_PRN222_Group7.Hubs;
using Assignment1_PRN222_Group7_BLL.Services;
using Assignment1_PRN222_Group7_DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7.Pages.User
{
    [Authorize(Policy = "AdminOnly")]
    public class AssignSubjectsModel : SubjectHubPageModel
    {
        private readonly IAccountService _accountService;
        private readonly ISubjectService _subjectService;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<AssignSubjectsModel> _logger;

        public AssignSubjectsModel(
            IAccountService accountService, 
            ISubjectService subjectService, 
            IHubContext<SubjectHub> hubContext,
            IWebHostEnvironment env,
            ILogger<AssignSubjectsModel> logger)
            : base(hubContext)
        {
            _accountService = accountService;
            _subjectService = subjectService;
            _env = env;
            _logger = logger;
        }

        public Assignment1_PRN222_Group7_DAL.Entities.User Lecturer { get; set; } = null!;

        public List<Assignment1_PRN222_Group7_DAL.Entities.Subject> AllSubjects { get; set; } = [];

        public HashSet<int> AssignedIds { get; set; } = [];

        [BindProperty]
        public List<int> SelectedSubjectIds { get; set; } = [];

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var user = await _accountService.GetUserByIdAsync(id);
            if (user == null || user.Role.Name != "Lecturer")
            {
                return NotFound();
            }

            Lecturer = user;
            var all = await _subjectService.GetAllSubjectsAsync();
            // Filter: Chỉ hiện môn chưa phân quyền HOẶC đang phân quyền cho giảng viên này
            AllSubjects = all.Where(s => s.LecturerId == null || s.LecturerId == id).ToList();

            var assigned = await _accountService.GetSubjectsAssignedToLecturerAsync(id);
            AssignedIds = assigned.Select(s => s.Id).ToHashSet();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var user = await _accountService.GetUserByIdAsync(id);
            if (user == null || user.Role.Name != "Lecturer")
            {
                return NotFound();
            }

            // Lấy danh sách môn học trước khi cập nhật
            var oldAssigned = await _accountService.GetSubjectsAssignedToLecturerAsync(id);
            var oldIds = oldAssigned.Select(s => s.Id).ToList();

            var success = await _accountService.AssignSubjectsToLecturerAsync(id, SelectedSubjectIds);
            if (!success)
            {
                TempData["Error"] = "Gặp lỗi khi phân quyền môn học.";
            }
            else
            {
                // Lấy danh sách môn học sau khi cập nhật
                var newAssigned = await _accountService.GetSubjectsAssignedToLecturerAsync(id);
                var newIds = newAssigned.Select(s => s.Id).ToList();

                // Danh sách các môn bị ảnh hưởng (được thêm mới hoặc bị bỏ gán)
                var affectedIds = oldIds.Union(SelectedSubjectIds).Union(newIds).Distinct().ToList();

                foreach (var subId in affectedIds)
                {
                    var sub = await _subjectService.GetSubjectByIdAsync(subId);
                    if (sub != null)
                    {
                        await NotifySubjectUpdated(sub);
                    }
                }

                // GHI LOG PHÂN QUYỀN MÔN HỌC (AUDIT LOG)
                try
                {
                    var allSubjects = await _subjectService.GetAllSubjectsAsync();
                    var subjectMap = allSubjects.ToDictionary(s => s.Id, s => $"{s.Code} - {s.Name}");

                    var newlyAssigned = SelectedSubjectIds.Except(oldIds).Select(sid => subjectMap.GetValueOrDefault(sid, $"ID_{sid}")).ToList();
                    var unassigned = oldIds.Except(SelectedSubjectIds).Select(sid => subjectMap.GetValueOrDefault(sid, $"ID_{sid}")).ToList();

                    var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Unknown";
                    var adminName = User.Identity?.Name ?? "Admin";

                    var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Admin (ID: {adminId}, Username: {adminName}) đã cập nhật phân quyền môn học cho Giảng viên \"{user.FullName}\" (ID: {id}, Email: {user.Email}):\n";
                    if (newlyAssigned.Any())
                    {
                        logMessage += $"  - GÁN THÊM môn học: {string.Join(", ", newlyAssigned)}\n";
                    }
                    if (unassigned.Any())
                    {
                        logMessage += $"  - GỠ GÁN môn học: {string.Join(", ", unassigned)}\n";
                    }
                    if (!newlyAssigned.Any() && !unassigned.Any())
                    {
                        logMessage += "  - Không có thay đổi nào.\n";
                    }

                    // Log ra console
                    _logger.LogInformation("{LogMessage}", logMessage);

                    // Log ra file
                    var logDir = Path.Combine(_env.WebRootPath, "logs");
                    Directory.CreateDirectory(logDir);
                    var logPath = Path.Combine(logDir, "subject_assignment_logs.txt");
                    await System.IO.File.AppendAllTextAsync(logPath, logMessage + "\n");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi ghi file log phân quyền môn học.");
                }

                TempData["Success"] = $"Đã cập nhật phân quyền môn học cho giảng viên \"{user.FullName}\".";
            }

            return RedirectToPage("./Index");
        }
    }
}
