using Assignment1_PRN222_Group7_BLL.Services;
using Assignment1_PRN222_Group7_BLL.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Text.Json;
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
            if (!ModelState.IsValid)
            {
                ViewData["ReturnUrl"] = returnUrl;
                return View();
            }
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
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Register()
        {
            ViewBag.Roles = await _accountService.GetAvailableRolesAsync();
            return View();
        }

        // POST /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Register(string fullName, string email, string? studentOrStaffId,
                                                   int roleId = 1)  // mặc định Student (RoleId=1)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Roles = await _accountService.GetAvailableRolesAsync();
                ViewBag.FullName = fullName;
                ViewBag.Email = email;
                ViewBag.StudentOrStaffId = studentOrStaffId;
                ViewBag.RoleId = roleId;
                return View();
            }

            // Thực hiện đăng ký thông qua BLL (BLL sẽ kiểm tra email và tự động gán gói Free)
            var success = await _accountService.RegisterAsync(fullName, email, null, roleId, studentOrStaffId);
            if (!success)
            {
                ModelState.AddModelError("email", "Email này đã được sử dụng.");
                ViewBag.Roles = await _accountService.GetAvailableRolesAsync();
                ViewBag.FullName = fullName;
                ViewBag.Email = email;
                ViewBag.StudentOrStaffId = studentOrStaffId;
                ViewBag.RoleId = roleId;
                return View();
            }

            TempData["Success"] = "Tạo tài khoản mới thành công! Thông tin tài khoản đã được gửi đến email.";
            return RedirectToAction(nameof(Register));
        }

        // POST /Account/RegisterBulk
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> RegisterBulk(IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
            {
                TempData["BulkError"] = "Vui lòng chọn một file Excel (.xlsx) để upload.";
                return RedirectToAction(nameof(Register));
            }

            var ext = Path.GetExtension(excelFile.FileName).ToLowerInvariant();
            if (ext != ".xlsx")
            {
                TempData["BulkError"] = "Chỉ hỗ trợ file định dạng Excel .xlsx";
                return RedirectToAction(nameof(Register));
            }

            using var stream = excelFile.OpenReadStream();
            var result = await _accountService.RegisterBulkAsync(stream);

            TempData["BulkSuccessCount"] = result.SuccessCount;
            TempData["BulkFailureCount"] = result.FailureCount;
            if (result.Errors.Count > 0)
            {
                // Serialize to JSON string vì TempData không lưu được List<string> trực tiếp
                TempData["BulkErrors"] = JsonSerializer.Serialize(result.Errors);
            }

            return RedirectToAction(nameof(Register));
        }

         // GET /Account/DownloadTemplate
        [HttpGet]
        [Authorize(Policy = "AdminOnly")]
        public IActionResult DownloadTemplate()
        {
            using var ms = new MemoryStream();
            using (var document = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook))
            {
                var workbookPart = document.AddWorkbookPart();
                workbookPart.Workbook = new Workbook();

                var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                worksheetPart.Worksheet = new Worksheet(new SheetData());

                var sheets = workbookPart.Workbook.AppendChild(new Sheets());
                var sheet = new Sheet() { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "Templates" };
                sheets.Append(sheet);

                var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();
                if (sheetData != null)
                {
                    // Add Header
                    var headerRow = new Row();
                    headerRow.Append(
                        new Cell() { DataType = CellValues.String, CellValue = new CellValue("Họ và Tên") },
                        new Cell() { DataType = CellValues.String, CellValue = new CellValue("Email") },
                        new Cell() { DataType = CellValues.String, CellValue = new CellValue("Vai trò") },
                        new Cell() { DataType = CellValues.String, CellValue = new CellValue("Mã số") }
                    );
                    sheetData.Append(headerRow);

                    // Add Sample Row 1
                    var sampleRow1 = new Row();
                    sampleRow1.Append(
                        new Cell() { DataType = CellValues.String, CellValue = new CellValue("Nguyễn Văn A") },
                        new Cell() { DataType = CellValues.String, CellValue = new CellValue("sv1@chatbot.edu.vn") },
                        new Cell() { DataType = CellValues.String, CellValue = new CellValue("Student") },
                        new Cell() { DataType = CellValues.String, CellValue = new CellValue("HE170001") }
                    );
                    sheetData.Append(sampleRow1);

                    // Add Sample Row 2
                    var sampleRow2 = new Row();
                    sampleRow2.Append(
                        new Cell() { DataType = CellValues.String, CellValue = new CellValue("Trần Thị B") },
                        new Cell() { DataType = CellValues.String, CellValue = new CellValue("gv1@chatbot.edu.vn") },
                        new Cell() { DataType = CellValues.String, CellValue = new CellValue("Lecturer") },
                        new Cell() { DataType = CellValues.String, CellValue = new CellValue("GV0001") }
                    );
                    sheetData.Append(sampleRow2);
                }

                workbookPart.Workbook.Save();
            }

            return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "UserRegistrationTemplate.xlsx");
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
