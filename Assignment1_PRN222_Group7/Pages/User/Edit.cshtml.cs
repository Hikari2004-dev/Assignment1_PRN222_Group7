using Assignment1_PRN222_Group7_BLL.Services;
using Assignment1_PRN222_Group7_DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7.Pages.User
{
    [Authorize(Policy = "AdminOnly")]
    public class EditModel : PageModel
    {
        private readonly IAccountService _accountService;

        public EditModel(IAccountService accountService)
        {
            _accountService = accountService;
        }

        [BindProperty]
        public int Id { get; set; }

        [BindProperty]
        public string FullName { get; set; } = string.Empty;

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        public int RoleId { get; set; }

        [BindProperty]
        public string? StudentOrStaffId { get; set; }

        [BindProperty]
        public string? Password { get; set; }

        [BindProperty]
        public string? ConfirmPassword { get; set; }

        public List<Role> Roles { get; set; } = [];

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var user = await _accountService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            Id = user.Id;
            FullName = user.FullName;
            Email = user.Email;
            RoleId = user.RoleId;
            StudentOrStaffId = user.StudentOrStaffId;

            Roles = await _accountService.GetAvailableRolesAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Email))
            {
                ModelState.AddModelError("", "Họ tên và Email không được để trống.");
                Roles = await _accountService.GetAvailableRolesAsync();
                return Page();
            }

            if (!string.IsNullOrEmpty(Password) && Password != ConfirmPassword)
            {
                ModelState.AddModelError("", "Mật khẩu xác nhận không khớp.");
                Roles = await _accountService.GetAvailableRolesAsync();
                return Page();
            }

            var user = await _accountService.GetUserByIdAsync(Id);
            if (user == null)
            {
                return NotFound();
            }

            user.FullName = FullName.Trim();
            user.Email = Email.Trim();
            user.RoleId = RoleId;
            user.StudentOrStaffId = StudentOrStaffId?.Trim();

            if (!string.IsNullOrEmpty(Password))
            {
                user.PasswordHash = Password;
            }
            else
            {
                user.PasswordHash = "";
            }

            var success = await _accountService.UpdateUserAsync(user);
            if (!success)
            {
                ModelState.AddModelError("", "Email này đã tồn tại ở tài khoản khác.");
                Roles = await _accountService.GetAvailableRolesAsync();
                return Page();
            }

            TempData["Success"] = $"Đã cập nhật thông tin tài khoản \"{user.FullName}\" thành công.";
            return RedirectToPage("./Index");
        }
    }
}
