using Assignment1_PRN222_Group7_DAL.Entities;

namespace Assignment1_PRN222_Group7_BLL.Services
{
    public interface IDocumentService
    {
        Task<IEnumerable<Document>> GetDocumentsBySubjectAsync(int subjectId, int? chapterId = null);
        Task<Document?> GetDocumentByIdAsync(int id);
        Task<Document> UploadDocumentAsync(Stream fileStream, string originalFileName, long fileSizeBytes,
            int subjectId, int? chapterId, string title, int userId, string webRootPath);
        Task<bool> DeleteDocumentAsync(int id, string webRootPath);
    }
}
