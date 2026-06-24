using Assignment1_PRN222_Group7.Hubs;
using Assignment1_PRN222_Group7_BLL.Services;
using Assignment1_PRN222_Group7_DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7.Pages.Subject
{
    [Authorize(Policy = "LecturerUp")]
    public class EditModel : SubjectHubPageModel
    {
        private readonly ISubjectService _subjectService;
        private readonly IAccountService _accountService;

        public EditModel(ISubjectService subjectService, IAccountService accountService, IHubContext<SubjectHub> hubContext) 
            : base(hubContext)
        {
            _subjectService = subjectService;
            _accountService = accountService;
        }

        [BindProperty]
        public int Id { get; set; }

        [BindProperty]
        public string Code { get; set; } = string.Empty;

        [BindProperty]
        public string Name { get; set; } = string.Empty;

        [BindProperty]
        public string? Description { get; set; }

        [BindProperty]
        public bool IsActive { get; set; }

        [BindProperty]
        public int? SelectedLecturerId { get; set; }

        public List<Assignment1_PRN222_Group7_DAL.Entities.User> Lecturers { get; set; } = [];

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var subject = await _subjectService.GetSubjectByIdAsync(id);
            if (subject == null)
            {
                return NotFound();
            }

            if (User.IsInRole("Lecturer"))
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var isAssigned = await _accountService.IsLecturerAssignedToSubjectAsync(userId, id);
                if (!isAssigned)
                {
                    return Forbid();
                }
            }

            Id = subject.Id;
            Code = subject.Code;
            Name = subject.Name;
            Description = subject.Description;
            IsActive = subject.IsActive;
            SelectedLecturerId = subject.LecturerId;

            var allUsers = await _accountService.GetAllUsersAsync();
            Lecturers = allUsers.Where(u => u.Role.Name == "Lecturer").ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var subject = await _subjectService.GetSubjectByIdAsync(Id);
            if (subject == null)
            {
                return NotFound();
            }

            if (User.IsInRole("Lecturer"))
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var isAssigned = await _accountService.IsLecturerAssignedToSubjectAsync(userId, Id);
                if (!isAssigned)
                {
                    return Forbid();
                }
            }

            if (string.IsNullOrWhiteSpace(Code) || string.IsNullOrWhiteSpace(Name))
            {
                ModelState.AddModelError(string.Empty, "Mã môn và tên môn không được để trống.");
                var allUsers = await _accountService.GetAllUsersAsync();
                Lecturers = allUsers.Where(u => u.Role.Name == "Lecturer").ToList();
                return Page();
            }

            subject.Code = Code.Trim().ToUpper();
            subject.Name = Name.Trim();
            subject.Description = Description?.Trim();
            subject.IsActive = IsActive;
            
            if (User.IsInRole("Admin"))
            {
                subject.LecturerId = SelectedLecturerId;
            }

            var ok = await _subjectService.UpdateSubjectAsync(subject);
            if (!ok)
            {
                ModelState.AddModelError("Code", $"Mã môn \"{Code}\" đã tồn tại ở môn khác.");
                var allUsers = await _accountService.GetAllUsersAsync();
                Lecturers = allUsers.Where(u => u.Role.Name == "Lecturer").ToList();
                return Page();
            }

            // SignalR broadcast
            await NotifySubjectUpdated(subject);

            TempData["Success"] = $"Đã cập nhật môn học \"{subject.Name}\".";
            return RedirectToPage("./Index");
        }
    }
}
