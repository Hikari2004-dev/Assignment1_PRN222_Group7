using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Assignment1_PRN222_Group7_DAL.Enums;

namespace Assignment1_PRN222_Group7_BLL.Services
{
    public class EmbeddingService : IEmbeddingService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<EmbeddingService> _logger;
        private readonly string _geminiApiKey;
        private readonly string _openAiApiKey;
        private readonly string _hfToken;

        public EmbeddingService(HttpClient httpClient, IConfiguration configuration, ILogger<EmbeddingService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _geminiApiKey = configuration["GeminiConfig:ApiKey"] ?? "";
            _openAiApiKey = configuration["OpenAiConfig:ApiKey"] ?? "";
            _hfToken = configuration["HuggingFace:Token"] ?? "";
        }

        public int GetEmbeddingDimensions(EmbeddingModel model)
        {
            return model switch
            {
                EmbeddingModel.MultilingualE5Base => 768,
                EmbeddingModel.TextEmbedding3Small => 1536,
                EmbeddingModel.PhoBERTBase => 768,
                EmbeddingModel.BgeM3 => 1024,
                EmbeddingModel.GeminiEmbedding004 => 768,
                _ => 768
            };
        }

        public async Task<float[]> GetEmbeddingAsync(string text, EmbeddingModel model)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new float[GetEmbeddingDimensions(model)];
            }

            try
            {
                return model switch
                {
                    EmbeddingModel.GeminiEmbedding004 => await GetGeminiEmbeddingAsync(text),
                    EmbeddingModel.TextEmbedding3Small => await GetOpenAiEmbeddingAsync(text),
                    EmbeddingModel.MultilingualE5Base => await GetHuggingFaceEmbeddingAsync(text, "intfloat/multilingual-e5-base"),
                    EmbeddingModel.PhoBERTBase => await GetHuggingFaceEmbeddingAsync(text, "symper/vietnamese-sbert"),
                    EmbeddingModel.BgeM3 => await GetHuggingFaceEmbeddingAsync(text, "BAAI/bge-m3"),
                    _ => await GetGeminiEmbeddingAsync(text)
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate embedding for model {Model}. Falling back to Gemini text-embedding-004.", model);
                try
                {
                    return await GetGeminiEmbeddingAsync(text);
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogError(fallbackEx, "All embedding providers failed. Returning zero vector as last resort.");
                    return new float[GetEmbeddingDimensions(model)];
                }
            }
        }

        private async Task<float[]> GetGeminiEmbeddingAsync(string text)
        {
            if (string.IsNullOrEmpty(_geminiApiKey))
            {
                throw new InvalidOperationException("Gemini API Key is not configured in GeminiConfig:ApiKey.");
            }

            var url = $"https://generativelanguage.googleapis.com/v1/models/text-embedding-004:embedContent?key={_geminiApiKey}";
            var payload = new
            {
                content = new { parts = new[] { new { text = text } } },
                taskType = "RETRIEVAL_DOCUMENT"
            };

            var response = await _httpClient.PostAsJsonAsync(url, payload);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Gemini Embedding API returned {response.StatusCode}: {err}");
            }

            var res = await response.Content.ReadFromJsonAsync<GeminiEmbeddingResponse>();
            if (res?.Embedding?.Values == null || res.Embedding.Values.Length == 0)
            {
                throw new Exception("Gemini Embedding API returned null or empty embedding.");
            }

            return res.Embedding.Values;
        }

        private async Task<float[]> GetOpenAiEmbeddingAsync(string text)
        {
            if (string.IsNullOrEmpty(_openAiApiKey))
            {
                throw new InvalidOperationException("OpenAI API Key is not configured.");
            }

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/embeddings");
            request.Headers.Add("Authorization", $"Bearer {_openAiApiKey}");
            request.Content = JsonContent.Create(new
            {
                model = "text-embedding-3-small",
                input = text
            });

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"OpenAI Embedding API returned {response.StatusCode}: {err}");
            }

            var res = await response.Content.ReadFromJsonAsync<OpenAiEmbeddingResponse>();
            if (res?.Data == null || res.Data.Count == 0 || res.Data[0].Embedding == null)
            {
                throw new Exception("OpenAI Embedding API returned empty data.");
            }

            return res.Data[0].Embedding!;
        }

        private async Task<float[]> GetHuggingFaceEmbeddingAsync(string text, string modelId)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"https://api-inference.huggingface.co/models/{modelId}");
            if (!string.IsNullOrEmpty(_hfToken))
            {
                request.Headers.Add("Authorization", $"Bearer {_hfToken}");
            }
            request.Content = JsonContent.Create(new { inputs = text });

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Hugging Face API ({modelId}) returned {response.StatusCode}: {err}");
            }

            var jsonDoc = await response.Content.ReadFromJsonAsync<JsonElement>();
            return ParseHuggingFaceResponse(jsonDoc);
        }

        private float[] ParseHuggingFaceResponse(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Array)
            {
                // If it's a flat array of floats
                if (element.GetArrayLength() > 0 && element[0].ValueKind == JsonValueKind.Number)
                {
                    var list = new List<float>();
                    foreach (var val in element.EnumerateArray())
                    {
                        list.Add(val.GetSingle());
                    }
                    return list.ToArray();
                }
                // If it is 2D array [[float, float, ...]]
                if (element.GetArrayLength() > 0 && element[0].ValueKind == JsonValueKind.Array)
                {
                    var inner = element[0];
                    if (inner.GetArrayLength() > 0 && inner[0].ValueKind == JsonValueKind.Number)
                    {
                        var list = new List<float>();
                        foreach (var val in inner.EnumerateArray())
                        {
                            list.Add(val.GetSingle());
                        }
                        return list.ToArray();
                    }
                    // If it is 3D array [[[float, float, ...]]] (BERT output)
                    if (inner.GetArrayLength() > 0 && inner[0].ValueKind == JsonValueKind.Array)
                    {
                        var tokenEmbeddings = inner;
                        int tokenCount = tokenEmbeddings.GetArrayLength();
                        if (tokenCount == 0) throw new Exception("Empty token embeddings from HuggingFace.");
                        
                        int dim = tokenEmbeddings[0].GetArrayLength();
                        var pooled = new float[dim];
                        for (int i = 0; i < tokenCount; i++)
                        {
                            var tokenVec = tokenEmbeddings[i];
                            for (int d = 0; d < dim; d++)
                            {
                                pooled[d] += tokenVec[d].GetSingle();
                            }
                        }
                        for (int d = 0; d < dim; d++)
                        {
                            pooled[d] /= tokenCount;
                        }
                        return pooled;
                    }
                }
            }

            throw new Exception("Unable to parse HuggingFace response: unknown shape.");
        }

        private float[] MatchDimension(float[] source, int targetDim)
        {
            if (source.Length == targetDim) return source;
            var result = new float[targetDim];
            if (source.Length < targetDim)
            {
                Array.Copy(source, result, source.Length);
            }
            else
            {
                Array.Copy(source, result, targetDim);
            }
            return result;
        }

        private class GeminiEmbeddingResponse
        {
            [JsonPropertyName("embedding")]
            public GeminiEmbeddingValues? Embedding { get; set; }
        }

        private class GeminiEmbeddingValues
        {
            [JsonPropertyName("values")]
            public float[]? Values { get; set; }
        }

        private class OpenAiEmbeddingResponse
        {
            [JsonPropertyName("data")]
            public List<OpenAiEmbeddingData>? Data { get; set; }
        }

        private class OpenAiEmbeddingData
        {
            [JsonPropertyName("embedding")]
            public float[]? Embedding { get; set; }
        }
    }
}
