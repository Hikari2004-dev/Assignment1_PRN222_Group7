using Assignment1_PRN222_Group7_DAL.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7_BLL.Services
{
    public record DocumentUploadModel(
        Stream FileStream,
        string OriginalFileName,
        long FileSizeBytes,
        int SubjectId,
        int? ChapterId,
        string Title,
        int UserId,
        string WebRootPath
    );

    public interface IDocumentService
    {
        Task<IEnumerable<Document>> GetDocumentsBySubjectAsync(int subjectId, int? chapterId = null);
        Task<Document?> GetDocumentByIdAsync(int id);
        Task<Document> UploadDocumentAsync(DocumentUploadModel model);
        Task<bool> DeleteDocumentAsync(int id, string webRootPath);
        Task<IEnumerable<DocumentChunk>> GetDocumentChunksAsync(int documentId);
    }
}
