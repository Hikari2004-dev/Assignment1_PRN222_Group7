using Assignment1_PRN222_Group7.Hubs;
using Assignment1_PRN222_Group7_BLL.Services;
using Assignment1_PRN222_Group7_DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7.Pages.Subject
{
    [Authorize(Policy = "LecturerUp")]
    public class CreateModel : SubjectHubPageModel
    {
        private readonly ISubjectService _subjectService;
        private readonly IAccountService _accountService;

        public CreateModel(ISubjectService subjectService, IAccountService accountService, IHubContext<SubjectHub> hubContext) 
            : base(hubContext)
        {
            _subjectService = subjectService;
            _accountService = accountService;
        }

        [BindProperty]
        public string Code { get; set; } = string.Empty;

        [BindProperty]
        public string Name { get; set; } = string.Empty;

        [BindProperty]
        public string? Description { get; set; }

        [BindProperty]
        public int? SelectedLecturerId { get; set; }

        public List<Assignment1_PRN222_Group7_DAL.Entities.User> Lecturers { get; set; } = [];

        public async Task<IActionResult> OnGetAsync()
        {
            var allUsers = await _accountService.GetAllUsersAsync();
            Lecturers = allUsers.Where(u => u.Role.Name == "Lecturer").ToList();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Code) || string.IsNullOrWhiteSpace(Name))
            {
                ModelState.AddModelError(string.Empty, "Mã môn và tên môn không được để trống.");
                var allUsers = await _accountService.GetAllUsersAsync();
                Lecturers = allUsers.Where(u => u.Role.Name == "Lecturer").ToList();
                return Page();
            }

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var lecturerId = User.IsInRole("Admin") ? SelectedLecturerId : (User.IsInRole("Lecturer") ? userId : null);

            var subject = new Assignment1_PRN222_Group7_DAL.Entities.Subject
            {
                Code = Code.Trim().ToUpper(),
                Name = Name.Trim(),
                Description = Description?.Trim(),
                CreatedBy = userId,
                LecturerId = lecturerId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var ok = await _subjectService.CreateSubjectAsync(subject);
            if (!ok)
            {
                ModelState.AddModelError("Code", $"Mã môn \"{Code}\" đã tồn tại.");
                var allUsers = await _accountService.GetAllUsersAsync();
                Lecturers = allUsers.Where(u => u.Role.Name == "Lecturer").ToList();
                return Page();
            }

            // SignalR broadcast
            await NotifySubjectCreated(subject);

            TempData["Success"] = $"Đã tạo môn học \"{subject.Name}\" thành công.";
            return RedirectToPage("./Index");
        }
    }
}
