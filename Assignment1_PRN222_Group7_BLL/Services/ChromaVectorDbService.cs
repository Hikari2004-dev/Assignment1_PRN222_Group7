using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Assignment1_PRN222_Group7_BLL.Services
{
    public class ChromaVectorDbService : IVectorDbService
    {
        private readonly HttpClient _http;
        private readonly ILogger<ChromaVectorDbService> _logger;

        public ChromaVectorDbService(HttpClient http, ILogger<ChromaVectorDbService> logger)
        {
            _http = http;
            _logger = logger;
        }

        public async Task<string?> UpsertAsync(string collectionName, string id, string text, Dictionary<string, string>? metadata = null)
        {
            try
            {
                var collectionId = await GetOrCreateCollectionAsync(collectionName);
                if (collectionId == null) return null;

                // Stub embedding: 768-dim random vector (demo — replace with real embedding service)
                var embedding = GenerateStubEmbedding(768);

                var payload = new
                {
                    ids = new[] { id },
                    documents = new[] { text },
                    metadatas = new[] { metadata ?? new Dictionary<string, string>() },
                    embeddings = new[] { embedding }
                };

                var response = await _http.PostAsJsonAsync($"/api/v1/collections/{collectionId}/upsert", payload);
                if (response.IsSuccessStatusCode)
                {
                    return id;
                }

                _logger.LogWarning("ChromaDB upsert failed: {Status}", response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ChromaDB upsert failed for id={Id}", id);
                return null;
            }
        }

        public async Task<bool> DeleteAsync(string collectionName, string id)
        {
            try
            {
                var collectionId = await GetOrCreateCollectionAsync(collectionName);
                if (collectionId == null) return false;

                var payload = new { ids = new[] { id } };
                var response = await _http.PostAsJsonAsync($"/api/v1/collections/{collectionId}/delete", payload);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ChromaDB delete failed for id={Id}", id);
                return false;
            }
        }

        public async Task<bool> DeleteByDocumentAsync(string collectionName, int documentId)
        {
            try
            {
                var collectionId = await GetOrCreateCollectionAsync(collectionName);
                if (collectionId == null) return false;

                var payload = new
                {
                    where = new Dictionary<string, object> { ["document_id"] = documentId.ToString() }
                };
                var response = await _http.PostAsJsonAsync($"/api/v1/collections/{collectionId}/delete", payload);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ChromaDB delete by document failed for docId={DocumentId}", documentId);
                return false;
            }
        }

        private async Task<string?> GetOrCreateCollectionAsync(string name)
        {
            try
            {
                var payload = new { name, get_or_create = true };
                var response = await _http.PostAsJsonAsync("/api/v1/collections", payload);
                if (!response.IsSuccessStatusCode) return null;

                var json = await response.Content.ReadFromJsonAsync<JsonElement>();
                return json.GetProperty("id").GetString();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ChromaDB get_or_create collection failed for {Name}", name);
                return null;
            }
        }

        private static float[] GenerateStubEmbedding(int dimensions)
        {
            var rng = Random.Shared;
            var vec = new float[dimensions];
            for (int i = 0; i < dimensions; i++)
                vec[i] = (float)(rng.NextDouble() * 2 - 1);
            return vec;
        }
    }
}
