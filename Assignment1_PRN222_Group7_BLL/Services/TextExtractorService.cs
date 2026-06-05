using Assignment1_PRN222_Group7_DAL.Enums;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using DrawingTable = DocumentFormat.OpenXml.Drawing.Table;
using DrawingTableRow = DocumentFormat.OpenXml.Drawing.TableRow;
using DrawingTableCell = DocumentFormat.OpenXml.Drawing.TableCell;
using System.Text;
using UglyToad.PdfPig;

namespace Assignment1_PRN222_Group7_BLL.Services
{
    public class TextExtractorService : ITextExtractorService
    {
        public async Task<string> ExtractTextAsync(string filePath, FileType fileType)
        {
            return fileType switch
            {
                FileType.PDF  => ExtractPdf(filePath),
                FileType.DOCX => ExtractDocx(filePath),
                FileType.PPTX => ExtractPptx(filePath),
                FileType.TXT  => await File.ReadAllTextAsync(filePath),
                _             => string.Empty
            };
        }

        private static string ExtractPdf(string filePath)
        {
            var sb = new StringBuilder();
            using var document = PdfDocument.Open(filePath);
            foreach (var page in document.GetPages())
            {
                sb.AppendLine(page.Text);
            }
            return sb.ToString();
        }

        private static string ExtractDocx(string filePath)
        {
            using var doc = WordprocessingDocument.Open(filePath, false);
            return doc.MainDocumentPart?.Document?.Body?.InnerText ?? string.Empty;
        }

        private static string ExtractPptx(string filePath)
        {
            var sb = new StringBuilder();
            using var pptx = PresentationDocument.Open(filePath, false);
            var presPart = pptx.PresentationPart;
            if (presPart?.Presentation?.SlideIdList == null) return string.Empty;

            foreach (var slideId in presPart.Presentation.SlideIdList.Elements<SlideId>())
            {
                var slidePart = (SlidePart?)presPart.GetPartById(slideId.RelationshipId!);
                if (slidePart?.Slide == null) continue;

                sb.AppendLine("=== Slide ===");
                ExtractAllText(slidePart.Slide, sb);
            }
            return sb.ToString();
        }

        private static void ExtractAllText(OpenXmlElement element, StringBuilder sb)
        {
            if (element is DrawingTable table)
            {
                foreach (var row in table.Elements<DrawingTableRow>())
                {
                    var cells = new List<string>();
                    foreach (var cell in row.Elements<DrawingTableCell>())
                    {
                        var cellText = GetElementText(cell);
                        if (!string.IsNullOrWhiteSpace(cellText))
                            cells.Add(cellText);
                    }
                    if (cells.Count > 0)
                        sb.AppendLine(string.Join(" | ", cells));
                }
                return;
            }

            var text = element.InnerText?.Trim();
            if (!string.IsNullOrWhiteSpace(text))
            {
                sb.AppendLine(text);
            }

            foreach (var child in element.Elements())
            {
                if (child is DrawingTable) continue;
                ExtractAllText(child, sb);
            }
        }

        private static string GetElementText(OpenXmlElement element)
        {
            var sb = new StringBuilder();
            foreach (var child in element.Elements())
            {
                var t = child.InnerText?.Trim();
                if (!string.IsNullOrWhiteSpace(t))
                    sb.Append(t).Append(' ');
            }
            return sb.ToString().Trim();
        }
    }
}
