using Assignment1_PRN222_Group7_DAL.Enums;

namespace Assignment1_PRN222_Group7_BLL.Services
{
    public interface ITextExtractorService
    {
        Task<string> ExtractTextAsync(string filePath, FileType fileType);
    }
}
