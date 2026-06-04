namespace Assignment1_PRN222_Group7_BLL.Services
{
    public interface IVectorDbService
    {
        /// <summary>Upsert a document chunk into vector DB. Returns embedding ID or null on failure.</summary>
        Task<string?> UpsertAsync(string collectionName, string id, string text, Dictionary<string, string>? metadata = null);

        /// <summary>Delete a single embedding by ID.</summary>
        Task<bool> DeleteAsync(string collectionName, string id);

        /// <summary>Delete all embeddings for a given document ID.</summary>
        Task<bool> DeleteByDocumentAsync(string collectionName, int documentId);

        /// <summary>Query vector database for similar items.</summary>
        Task<List<VectorSearchResult>> QueryAsync(string collectionName, float[] queryEmbedding, int limit = 5);
    }

    public class VectorSearchResult
    {
        public string Id { get; set; } = "";
        public double Score { get; set; }
    }
}
