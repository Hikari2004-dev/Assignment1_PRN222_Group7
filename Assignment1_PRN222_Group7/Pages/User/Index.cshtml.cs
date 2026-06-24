using Assignment1_PRN222_Group7_BLL.Services;
using Assignment1_PRN222_Group7_DAL.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7.Pages.User
{
    [Authorize(Policy = "AdminOnly")]
    public class IndexModel : PageModel
    {
        private readonly IAccountService _accountService;

        public IndexModel(IAccountService accountService)
        {
            _accountService = accountService;
        }

        public List<Assignment1_PRN222_Group7_DAL.Entities.User> Users { get; set; } = [];

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? RoleFilter { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var allUsers = await _accountService.GetAllUsersAsync();
            var queryUsers = allUsers.ToList();

            if (!string.IsNullOrWhiteSpace(Search))
            {
                var q = Search.Trim().ToLower();
                queryUsers = queryUsers.Where(u => u.FullName.ToLower().Contains(q) || u.Email.ToLower().Contains(q)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(RoleFilter))
            {
                queryUsers = queryUsers.Where(u => u.Role.Name == RoleFilter).ToList();
            }

            Users = queryUsers;
            return Page();
        }

        public async Task<IActionResult> OnPostToggleStatusAsync(int id)
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
            return RedirectToPage(new { Search, RoleFilter });
        }
    }
}
