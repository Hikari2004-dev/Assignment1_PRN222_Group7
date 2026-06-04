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

        public async Task<List<VectorSearchResult>> QueryAsync(string collectionName, float[] queryEmbedding, int limit = 5)
        {
            try
            {
                var collectionId = await GetOrCreateCollectionAsync(collectionName);
                if (collectionId == null) return new List<VectorSearchResult>();

                var payload = new
                {
                    query_embeddings = new[] { queryEmbedding },
                    n_results = limit,
                    include = new[] { "distances" }
                };

                var response = await _http.PostAsJsonAsync($"/api/v1/collections/{collectionId}/query", payload);
                if (response.IsSuccessStatusCode)
                {
                    var responseData = await response.Content.ReadFromJsonAsync<ChromaQueryResponse>();
                    if (responseData?.Ids != null && responseData.Ids.Count > 0 && responseData.Ids[0] != null)
                    {
                        var results = new List<VectorSearchResult>();
                        for (int i = 0; i < responseData.Ids[0].Count; i++)
                        {
                            var score = responseData.Distances != null && responseData.Distances.Count > 0 && responseData.Distances[0].Count > i
                                ? responseData.Distances[0][i]
                                : 1.0;

                            // Chroma returns distance (lower is better). We can represent similarity as 1 - distance, or just return the raw score.
                            // Since we want to store it in MessageSource, let's store the similarity score.
                            results.Add(new VectorSearchResult
                            {
                                Id = responseData.Ids[0][i],
                                Score = 1.0 - score // Convert distance to similarity
                            });
                        }
                        return results;
                    }
                }

                _logger.LogWarning("ChromaDB query failed: {Status}", response.StatusCode);
                return new List<VectorSearchResult>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ChromaDB query failed for collection={Collection}", collectionName);
                return new List<VectorSearchResult>();
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

    public class ChromaQueryResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("ids")]
        public List<List<string>>? Ids { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("distances")]
        public List<List<double>>? Distances { get; set; }
    }
}
