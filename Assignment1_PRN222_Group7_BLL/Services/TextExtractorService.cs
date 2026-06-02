using Assignment1_PRN222_Group7_DAL.Enums;
using UglyToad.PdfPig;
using DocumentFormat.OpenXml.Packaging;
using System.Text;

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
            using var doc = PresentationDocument.Open(filePath, false);
            var presentationPart = doc.PresentationPart;
            if (presentationPart?.Presentation?.SlideIdList == null) return string.Empty;

            foreach (var slideId in presentationPart.Presentation.SlideIdList.Elements<DocumentFormat.OpenXml.Presentation.SlideId>())
            {
                var slidePart = (SlidePart?)presentationPart.GetPartById(slideId.RelationshipId!);
                if (slidePart?.Slide != null)
                {
                    sb.AppendLine(slidePart.Slide.InnerText);
                }
            }
            return sb.ToString();
        }
    }
}
