using Assignment1_PRN222_Group7_BLL.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAccountService _accountService;

        public AccountController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        // GET /Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToLocal(returnUrl);

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password,
                                               bool rememberMe = false,
                                               string? returnUrl = null)
        {
            // 1. Xác thực thông qua BLL
            var user = await _accountService.LoginAsync(email, password);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
                ViewData["ReturnUrl"] = returnUrl;
                return View();
            }

            // 2. Tạo Claims
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name,           user.FullName),
                new(ClaimTypes.Email,          user.Email),
                new(ClaimTypes.Role,           user.Role.Name), // "Admin" | "Lecturer" | "Student"
            };

            var identity  = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            // 3. Ghi Cookie
            var props = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc   = rememberMe
                    ? DateTimeOffset.UtcNow.AddDays(30)
                    : DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal, props);

            // 4. Cập nhật LastLoginAt qua BLL
            await _accountService.UpdateLastLoginAsync(user.Id);

            return RedirectToLocal(returnUrl);
        }

        // POST /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        // GET /Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // GET /Account/Register
        [HttpGet]
        public async Task<IActionResult> Register()
        {
            ViewBag.Roles = await _accountService.GetAvailableRolesAsync();
            return View();
        }

        // POST /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string fullName, string email,
                                                   string password, string confirmPassword,
                                                   int roleId = 1)  // mặc định Student (RoleId=1)
        {
            // Validate mật khẩu khớp
            if (password != confirmPassword)
            {
                ModelState.AddModelError("confirmPassword", "Mật khẩu xác nhận không khớp.");
                return View();
            }

            // Thực hiện đăng ký thông qua BLL (BLL sẽ kiểm tra email và tự động gán gói Free)
            var success = await _accountService.RegisterAsync(fullName, email, password, roleId);
            if (!success)
            {
                ModelState.AddModelError("email", "Email này đã được sử dụng.");
                return View();
            }

            TempData["Success"] = "Đăng ký thành công! Hãy đăng nhập.";
            return RedirectToAction("Login");
        }

        // ─── Helper ───────────────────────────────────────────────────────────
        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index", "Home");
        }
    }
}
