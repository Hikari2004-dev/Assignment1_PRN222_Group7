using Assignment1_PRN222_Group7.Hubs;
using Assignment1_PRN222_Group7_BLL.Services;
using Assignment1_PRN222_Group7_DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7.Pages.User
{
    [Authorize(Policy = "AdminOnly")]
    public class AssignSubjectsModel : SubjectHubPageModel
    {
        private readonly IAccountService _accountService;
        private readonly ISubjectService _subjectService;

        public AssignSubjectsModel(IAccountService accountService, ISubjectService subjectService, IHubContext<SubjectHub> hubContext)
            : base(hubContext)
        {
            _accountService = accountService;
            _subjectService = subjectService;
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

                TempData["Success"] = $"Đã cập nhật phân quyền môn học cho giảng viên \"{user.FullName}\".";
            }

            return RedirectToPage("./Index");
        }
    }
}
