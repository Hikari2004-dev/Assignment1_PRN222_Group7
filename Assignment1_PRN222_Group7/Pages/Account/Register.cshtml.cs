using Assignment1_PRN222_Group7_BLL.Services;
using Assignment1_PRN222_Group7_DAL.Entities;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7.Pages.Account
{
    [Authorize(Policy = "AdminOnly")]
    public class RegisterModel : PageModel
    {
        private readonly IAccountService _accountService;

        public RegisterModel(IAccountService accountService)
        {
            _accountService = accountService;
        }

        public List<Role> Roles { get; set; } = [];

        [BindProperty]
        public string FullName { get; set; } = string.Empty;

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        public string? StudentOrStaffId { get; set; }

        [BindProperty]
        public int RoleId { get; set; } = 1;

        public async Task<IActionResult> OnGetAsync()
        {
            Roles = await _accountService.GetAvailableRolesAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostRegisterAsync()
        {
            if (!ModelState.IsValid)
            {
                Roles = await _accountService.GetAvailableRolesAsync();
                return Page();
            }

            var success = await _accountService.RegisterAsync(FullName, Email, null, RoleId, StudentOrStaffId);
            if (!success)
            {
                ModelState.AddModelError("Email", "Email này đã được sử dụng.");
                Roles = await _accountService.GetAvailableRolesAsync();
                return Page();
            }

            TempData["Success"] = "Tạo tài khoản mới thành công! Thông tin tài khoản đã được gửi đến email.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRegisterBulkAsync(IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
            {
                TempData["BulkError"] = "Vui lòng chọn một file Excel (.xlsx) để upload.";
                return RedirectToPage();
            }

            var ext = Path.GetExtension(excelFile.FileName).ToLowerInvariant();
            if (ext != ".xlsx")
            {
                TempData["BulkError"] = "Chỉ hỗ trợ file định dạng Excel .xlsx";
                return RedirectToPage();
            }

            using var stream = excelFile.OpenReadStream();
            var result = await _accountService.RegisterBulkAsync(stream);

            TempData["BulkSuccessCount"] = result.SuccessCount;
            TempData["BulkFailureCount"] = result.FailureCount;
            if (result.Errors.Count > 0)
            {
                TempData["BulkErrors"] = JsonSerializer.Serialize(result.Errors);
            }

            return RedirectToPage();
        }

        public IActionResult OnGetDownloadTemplate()
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
    }
}
