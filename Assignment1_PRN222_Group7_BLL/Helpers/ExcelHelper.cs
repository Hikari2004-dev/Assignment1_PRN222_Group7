using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Assignment1_PRN222_Group7_BLL.Helpers
{
    public static class ExcelHelper
    {
        public static List<Dictionary<string, string>> ReadExcelRows(Stream stream)
        {
            var list = new List<Dictionary<string, string>>();
            try
            {
                using var doc = SpreadsheetDocument.Open(stream, false);
                var workbookPart = doc.WorkbookPart;
                if (workbookPart == null) return list;

                var sheet = workbookPart.Workbook.Sheets?.GetFirstChild<Sheet>();
                if (sheet == null || sheet.Id?.Value == null) return list;

                var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id.Value);
                var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();
                if (sheetData == null) return list;

                var stringTable = workbookPart.SharedStringTablePart?.SharedStringTable;

                // First row is header
                var headerRow = sheetData.Elements<Row>().FirstOrDefault();
                if (headerRow == null) return list;

                var headers = new Dictionary<string, string>(); // Column Letter -> Header Name normalized
                int colIdx = 0;
                foreach (Cell cell in headerRow.Elements<Cell>())
                {
                    var colLetter = GetColumnLetterForCell(cell, colIdx++);
                    var val = GetCellValue(cell, stringTable);
                    if (!string.IsNullOrWhiteSpace(val) && !string.IsNullOrWhiteSpace(colLetter))
                    {
                        var normHeader = NormalizeHeader(val);
                        headers[colLetter] = normHeader;
                    }
                }

                foreach (Row row in sheetData.Elements<Row>().Skip(1))
                {
                    var rowData = new Dictionary<string, string>();
                    int cellIdx = 0;
                    foreach (Cell cell in row.Elements<Cell>())
                    {
                        var colLetter = GetColumnLetterForCell(cell, cellIdx++);
                        if (!string.IsNullOrWhiteSpace(colLetter) && headers.TryGetValue(colLetter, out var headerName))
                        {
                            var val = GetCellValue(cell, stringTable);
                            rowData[headerName] = val?.Trim() ?? "";
                        }
                    }
                    if (rowData.Count > 0 && rowData.Values.Any(v => !string.IsNullOrWhiteSpace(v)))
                    {
                        list.Add(rowData);
                    }
                }
            }
            catch (Exception)
            {
                // Return empty list if excel parsing fails
            }
            return list;
        }

        private static string GetColumnLetterForCell(Cell cell, int index)
        {
            var cellRef = cell.CellReference?.Value;
            if (!string.IsNullOrEmpty(cellRef))
            {
                return new string(cellRef.TakeWhile(char.IsLetter).ToArray());
            }
            return GetColumnLetterFromNumber(index + 1);
        }

        private static string GetColumnLetterFromNumber(int columnNumber)
        {
            string columnName = "";
            while (columnNumber > 0)
            {
                int modulo = (columnNumber - 1) % 26;
                columnName = Convert.ToChar(65 + modulo) + columnName;
                columnNumber = (columnNumber - modulo) / 26;
            }
            return columnName;
        }

        private static string GetCellValue(Cell cell, SharedStringTable? stringTable)
        {
            var val = cell.CellValue?.Text ?? cell.InnerText ?? "";
            if (cell.DataType != null && stringTable != null)
            {
                var type = cell.DataType.Value;
                if (type == CellValues.SharedString)
                {
                    if (int.TryParse(val, out int index))
                    {
                        return stringTable.ElementAt(index).InnerText;
                    }
                }
                else if (type == CellValues.Boolean)
                {
                    return val == "0" ? "FALSE" : "TRUE";
                }
            }
            return val;
        }

        private static string NormalizeHeader(string header)
        {
            header = header.Trim().ToLowerInvariant();
            
            // Map variations of FullName
            if (header == "họ và tên" || header == "ho va ten" || header == "fullname" || header == "full name" || header == "họ tên")
                return "FullName";

            // Map variations of Email
            if (header == "email" || header == "địa chỉ email" || header == "dia chi email" || header == "mail")
                return "Email";

            // Map variations of Password
            if (header == "mật khẩu" || header == "mat khau" || header == "password" || header == "pass")
                return "Password";

            // Map variations of Role
            if (header == "vai trò" || header == "vai tro" || header == "role" || header == "chức vụ" || header == "chuc vu")
                return "Role";

            // Map variations of StudentOrStaffId
            if (header == "mã số" || header == "ma so" || header == "mssv" || header == "student id" || header == "staff id" || header == "id" || header == "mã số sinh viên" || header == "mã giảng viên")
                return "StudentOrStaffId";

            return header;
        }
    }
}
