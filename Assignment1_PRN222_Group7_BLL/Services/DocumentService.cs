using Assignment1_PRN222_Group7_DAL.Entities;
using Assignment1_PRN222_Group7_DAL.Enums;
using Assignment1_PRN222_Group7_DAL.Repositories;

namespace Assignment1_PRN222_Group7_BLL.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly IUnitOfWork _uow;

        public DocumentService(IUnitOfWork uow) => _uow = uow;

        public async Task<IEnumerable<Document>> GetDocumentsBySubjectAsync(int subjectId, int? chapterId = null)
        {
            var repo = _uow.GetRepository<Document>();
            var docs = chapterId.HasValue
                ? await repo.FindAsync(d => d.SubjectId == subjectId && d.ChapterId == chapterId.Value, "Subject", "Chapter", "Uploader")
                : await repo.FindAsync(d => d.SubjectId == subjectId, "Subject", "Chapter", "Uploader");
            return docs.OrderByDescending(d => d.UploadedAt);
        }

        public async Task<Document?> GetDocumentByIdAsync(int id)
        {
            var repo = _uow.GetRepository<Document>();
            return await repo.FirstOrDefaultAsync(d => d.Id == id, "Subject", "Chapter", "Uploader");
        }

        public async Task<Document> UploadDocumentAsync(DocumentUploadModel model)
        {
            // Determine file type from extension
            var ext = Path.GetExtension(model.OriginalFileName).ToLowerInvariant();
            var fileType = ext switch
            {
                ".pdf"  => FileType.PDF,
                ".docx" => FileType.DOCX,
                ".pptx" => FileType.PPTX,
                ".txt"  => FileType.TXT,
                _       => FileType.Other
            };

            // Generate unique stored file name
            var storedFileName = $"{Guid.NewGuid()}{ext}";
            var uploadDir = Path.Combine(model.WebRootPath, "uploads", "documents");
            Directory.CreateDirectory(uploadDir);
            var filePath = Path.Combine(uploadDir, storedFileName);

            // Save file to disk
            using (var output = new FileStream(filePath, FileMode.Create))
            {
                await model.FileStream.CopyToAsync(output);
            }

            var doc = new Document
            {
                Title            = model.Title.Trim(),
                OriginalFileName = model.OriginalFileName,
                StoredFileName   = storedFileName,
                FilePath         = Path.Combine("uploads", "documents", storedFileName),
                FileType         = fileType,
                FileSizeBytes    = model.FileSizeBytes,
                SubjectId        = model.SubjectId,
                ChapterId        = model.ChapterId,
                UploadedBy       = model.UserId,
                UploadedAt       = DateTime.UtcNow,
                IsIndexed        = false,
                TotalChunks      = 0
            };

            await _uow.GetRepository<Document>().AddAsync(doc);
            await _uow.SaveChangesAsync();
            return doc;
        }

        public async Task<bool> DeleteDocumentAsync(int id, string webRootPath)
        {
            var repo = _uow.GetRepository<Document>();
            var doc = await repo.GetByIdAsync(id);
            if (doc == null) return false;

            // Delete physical file
            var fullPath = Path.Combine(webRootPath, doc.FilePath);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            // DB record + chunks cascade-deleted via EF config
            repo.Remove(doc);
            await _uow.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<DocumentChunk>> GetDocumentChunksAsync(int documentId)
        {
            var repo = _uow.GetRepository<DocumentChunk>();
            var chunks = await repo.FindAsync(c => c.DocumentId == documentId);
            return chunks.OrderBy(c => c.ChunkIndex);
        }
    }
}
