using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Assignment1_PRN222_Group7_DAL.Entities;
using Assignment1_PRN222_Group7_DAL.Enums;

namespace Assignment1_PRN222_Group7_BLL.Services
{
    public class GeminiService : IAiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GeminiService> _logger;
        private readonly string _apiKey;
        private readonly string _model;
        private readonly string _baseUrl;

        public GeminiService(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = configuration["GeminiConfig:ApiKey"] ?? "";
            _model = configuration["GeminiConfig:Model"] ?? "gemini-1.5-flash";
            _baseUrl = configuration["GeminiConfig:BaseUrl"] ?? "https://generativelanguage.googleapis.com";
        }

        public async Task<string> GenerateContentAsync(string prompt, List<ChatMessage>? history = null)
        {
            try
            {
                var contents = new List<GeminiContent>();

                if (history != null)
                {
                    foreach (var msg in history)
                    {
                        if (msg.Role == MessageRole.User)
                        {
                            contents.Add(new GeminiContent
                            {
                                Role = "user",
                                Parts = new List<GeminiPart> { new GeminiPart { Text = msg.Content } }
                            });
                        }
                        else if (msg.Role == MessageRole.Assistant)
                        {
                            contents.Add(new GeminiContent
                            {
                                Role = "model",
                                Parts = new List<GeminiPart> { new GeminiPart { Text = msg.Content } }
                            });
                        }
                    }
                }

                // Add the current prompt
                contents.Add(new GeminiContent
                {
                    Role = "user",
                    Parts = new List<GeminiPart> { new GeminiPart { Text = prompt } }
                });

                var payload = new GeminiRequest
                {
                    Contents = contents,
                    SystemInstruction = new GeminiSystemInstruction
                    {
                        Parts = new List<GeminiPart>
                        {
                            new GeminiPart { Text = "You are a professional educational assistant for college subjects. Always answer in the language requested by the user, using clear explanation, structured formatting, and citing sources when available. Do not invent facts, only use the provided context to answer questions when possible." }
                        }
                    }
                };

                var url = $"{_baseUrl.TrimEnd('/')}/v1beta/models/{_model}:generateContent?key={_apiKey}";
                var response = await _httpClient.PostAsJsonAsync(url, payload);

                if (!response.IsSuccessStatusCode)
                {
                    var errBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Gemini API error. Status: {StatusCode}, Body: {Body}", response.StatusCode, errBody);
                    throw new HttpRequestException($"Gemini API error: {response.StatusCode} - {errBody}");
                }

                var responseData = await response.Content.ReadFromJsonAsync<GeminiResponse>();
                var generatedText = responseData?.Candidates?[0]?.Content?.Parts?[0]?.Text;

                if (string.IsNullOrEmpty(generatedText))
                {
                    _logger.LogWarning("Gemini API returned an empty response. Response JSON: {Response}", JsonSerializer.Serialize(responseData));
                    return "Sorry, I could not generate a response.";
                }

                return generatedText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to call Gemini API");
                throw;
            }
        }

        // Inner classes for API request/response
        private class GeminiRequest
        {
            [JsonPropertyName("contents")]
            public List<GeminiContent> Contents { get; set; } = new();

            [JsonPropertyName("systemInstruction")]
            public GeminiSystemInstruction? SystemInstruction { get; set; }
        }

        private class GeminiSystemInstruction
        {
            [JsonPropertyName("parts")]
            public List<GeminiPart> Parts { get; set; } = new();
        }

        private class GeminiContent
        {
            [JsonPropertyName("role")]
            public string Role { get; set; } = "";

            [JsonPropertyName("parts")]
            public List<GeminiPart> Parts { get; set; } = new();
        }

        private class GeminiPart
        {
            [JsonPropertyName("text")]
            public string Text { get; set; } = "";
        }

        private class GeminiResponse
        {
            [JsonPropertyName("candidates")]
            public List<GeminiCandidate>? Candidates { get; set; }
        }

        private class GeminiCandidate
        {
            [JsonPropertyName("content")]
            public GeminiContent? Content { get; set; }
        }
    }
}
