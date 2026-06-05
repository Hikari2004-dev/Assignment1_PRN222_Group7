using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Assignment1_PRN222_Group7_BLL.Services;

public class ChromaVectorDbService : IVectorDbService
{
    private readonly HttpClient _http;
    private readonly ILogger<ChromaVectorDbService> _logger;
    private readonly string _baseUrl;
    private readonly string _tenant;
    private readonly string _database;
    private readonly Dictionary<string, string> _collectionIdCache = new();
    private readonly object _cacheLock = new();

    public ChromaVectorDbService(HttpClient http, ILogger<ChromaVectorDbService> logger)
    {
        _http = http;
        _logger = logger;
        var baseUrl = Environment.GetEnvironmentVariable("CHROMADB_URL") ?? "http://hikari2004.ddns.net:8017";
        _tenant = Environment.GetEnvironmentVariable("CHROMADB_TENANT") ?? "default";
        _database = Environment.GetEnvironmentVariable("CHROMADB_DATABASE") ?? "chromadb";
        _baseUrl = baseUrl.TrimEnd('/');
    }

    private string TenantUrl => $"{_baseUrl}/api/v2/tenants/{_tenant}";
    private string DatabaseUrl => $"{TenantUrl}/databases/{_database}";
    private string CollectionsUrl => $"{DatabaseUrl}/collections";

    private async Task EnsureDatabaseExistsAsync()
    {
        var url = $"{TenantUrl}/databases";
        var payload = new { name = _database };

        var response = await _http.PostAsJsonAsync(url, payload);
        if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.Conflict)
        {
            var err = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Failed to ensure database exists at {Url}: {Status} {Body}", url, response.StatusCode, err);
        }
    }

    private async Task<string?> GetCollectionIdAsync(string name)
    {
        lock (_cacheLock)
        {
            if (_collectionIdCache.TryGetValue(name, out var cached))
                return cached;
        }

        var url = CollectionsUrl;
        var response = await _http.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to list collections: {Status}", response.StatusCode);
            return null;
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        if (json.ValueKind != JsonValueKind.Array) return null;

        foreach (var item in json.EnumerateArray())
        {
            if (item.TryGetProperty("name", out var nameProp) &&
                nameProp.GetString() == name &&
                item.TryGetProperty("id", out var idProp))
            {
                var id = idProp.GetString();
                if (id != null)
                {
                    lock (_cacheLock)
                    {
                        _collectionIdCache[name] = id;
                    }
                    return id;
                }
            }
        }

        return null;
    }

    private async Task<string?> GetOrCreateCollectionAsync(string name)
    {
        var existingId = await GetCollectionIdAsync(name);
        if (existingId != null) return existingId;

        var url = CollectionsUrl;
        var payload = new { name, get_or_create = true };

        var response = await _http.PostAsJsonAsync(url, payload);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Failed to create collection {Name}: {Status} {Body}", name, response.StatusCode, err);
            return null;
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        if (json.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.String)
        {
            var id = idProp.GetString();
            if (id != null)
            {
                lock (_cacheLock)
                {
                    _collectionIdCache[name] = id;
                }
                return id;
            }
        }

        return await GetCollectionIdAsync(name);
    }

    public async Task<string?> UpsertAsync(string collectionName, string id, float[] embedding, string text, Dictionary<string, string>? metadata = null)
    {
        await EnsureDatabaseExistsAsync();
        var collectionId = await GetOrCreateCollectionAsync(collectionName);
        if (collectionId == null) return null;

        try
        {
            var payload = new
            {
                ids = new[] { id },
                documents = new[] { text },
                embeddings = new[] { embedding.Select(e => (double)e).ToArray() },
                metadatas = new[] { metadata ?? new Dictionary<string, string>() }
            };

            var url = $"{DatabaseUrl}/collections/{collectionId}/upsert";
            var response = await _http.PostAsJsonAsync(url, payload);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("ChromaDB upsert failed: {Status} {Body}", response.StatusCode, err);
                return null;
            }

            return id;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ChromaDB upsert failed for id={Id}", id);
            return null;
        }
    }

    public async Task<bool> DeleteAsync(string collectionName, string id)
    {
        var collectionId = await GetCollectionIdAsync(collectionName);
        if (collectionId == null) return false;

        try
        {
            var payload = new { ids = new[] { id } };
            var url = $"{DatabaseUrl}/collections/{collectionId}/delete";
            var response = await _http.PostAsJsonAsync(url, payload);
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
        var collectionId = await GetCollectionIdAsync(collectionName);
        if (collectionId == null) return false;

        try
        {
            var payload = new
            {
                where = new Dictionary<string, object> { ["document_id"] = documentId.ToString() }
            };
            var url = $"{DatabaseUrl}/collections/{collectionId}/delete";
            var response = await _http.PostAsJsonAsync(url, payload);
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
        var collectionId = await GetCollectionIdAsync(collectionName);
        if (collectionId == null) return new List<VectorSearchResult>();

        try
        {
            var payload = new
            {
                query_embeddings = new[] { queryEmbedding.Select(e => (double)e).ToArray() },
                n_results = limit,
                include = new[] { "documents", "metadatas", "distances" }
            };

            var url = $"{DatabaseUrl}/collections/{collectionId}/query";
            var response = await _http.PostAsJsonAsync(url, payload);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("ChromaDB query failed: {Status} {Body}", response.StatusCode, err);
                return new List<VectorSearchResult>();
            }

            var json = await response.Content.ReadFromJsonAsync<ChromaQueryResponse>();
            if (json?.Ids == null || json.Ids.Count == 0 || json.Ids[0] == null)
                return new List<VectorSearchResult>();

            var results = new List<VectorSearchResult>();
            for (int i = 0; i < json.Ids[0].Count; i++)
            {
                var distance = json.Distances != null && json.Distances.Count > 0 && json.Distances[0].Count > i
                    ? json.Distances[0][i]
                    : 1.0;

                var docText = json.Documents != null && json.Documents.Count > 0 && json.Documents[0].Count > i
                    ? json.Documents[0][i] ?? ""
                    : "";

                var meta = json.Metadatas != null && json.Metadatas.Count > 0 && json.Metadatas[0].Count > i
                    ? json.Metadatas[0][i] ?? new Dictionary<string, string>()
                    : new Dictionary<string, string>();

                results.Add(new VectorSearchResult
                {
                    Id = json.Ids[0][i],
                    Score = 1.0 - Math.Max(0, Math.Min(1, distance)),
                    Document = docText,
                    Metadata = meta
                });
            }
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ChromaDB query failed for collection={Collection}", collectionName);
            return new List<VectorSearchResult>();
        }
    }
}

public class ChromaQueryResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("ids")]
    public List<List<string>>? Ids { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("distances")]
    public List<List<double>>? Distances { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("documents")]
    public List<List<string?>>? Documents { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("metadatas")]
    public List<List<Dictionary<string, string>?>>? Metadatas { get; set; }
}
