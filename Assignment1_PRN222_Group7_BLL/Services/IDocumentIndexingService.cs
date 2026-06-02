namespace Assignment1_PRN222_Group7_BLL.Services
{
    public interface IDocumentIndexingService
    {
        /// <summary>Extract text, chunk, embed, and upsert to vector DB for a document.</summary>
        Task IndexDocumentAsync(int documentId, string webRootPath);
    }
}
