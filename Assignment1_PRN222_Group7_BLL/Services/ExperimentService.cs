using Assignment1_PRN222_Group7_DAL.Entities;
using Assignment1_PRN222_Group7_DAL.Enums;
using Assignment1_PRN222_Group7_DAL.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7_BLL.Services
{
    /// <summary>
    /// Experiment business logic service.
    /// </summary>
    public class ExperimentService : IExperimentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IVectorDbService _vectorDb;
        private readonly IAiService _aiService;
        private readonly IChunkingService _chunking;
        private readonly IEmbeddingService _embeddingService;
        private readonly ITextExtractorService _textExtractor;

        public ExperimentService(
            IUnitOfWork unitOfWork,
            IVectorDbService vectorDb,
            IAiService aiService,
            IChunkingService chunking,
            IEmbeddingService embeddingService,
            ITextExtractorService textExtractor)
        {
            _unitOfWork = unitOfWork;
            _vectorDb = vectorDb;
            _aiService = aiService;
            _chunking = chunking;
            _embeddingService = embeddingService;
            _textExtractor = textExtractor;
        }

        #region Experiment CRUD

        public async Task<IEnumerable<Experiment>> GetAllExperimentsAsync()
        {
            var repo = _unitOfWork.GetRepository<Experiment>();
            return await repo.FindAsync(e => true, "Subject", "Creator", "Configurations", "TestQuestions", "Results");
        }

        public async Task<Experiment?> GetExperimentByIdAsync(int id)
        {
            var repo = _unitOfWork.GetRepository<Experiment>();
            return await repo.FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<Experiment?> GetExperimentWithDetailsAsync(int id)
        {
            var repo = _unitOfWork.GetRepository<Experiment>();
            return await repo.FirstOrDefaultAsync(
                e => e.Id == id,
                "Subject",
                "Creator",
                "Configurations",
                "TestQuestions",
                "Results",
                "Results.Configuration",
                "Results.Question"
            );
        }

        public async Task<bool> CreateExperimentAsync(Experiment experiment)
        {
            try
            {
                experiment.CreatedAt = DateTime.UtcNow;
                experiment.Status = ExperimentStatus.Draft;

                var repo = _unitOfWork.GetRepository<Experiment>();
                await repo.AddAsync(experiment);
                await _unitOfWork.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateExperimentAsync(Experiment experiment)
        {
            try
            {
                var repo = _unitOfWork.GetRepository<Experiment>();
                repo.Update(experiment);
                await _unitOfWork.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteExperimentAsync(int id)
        {
            try
            {
                var repo = _unitOfWork.GetRepository<Experiment>();
                var experiment = await repo.GetByIdAsync(id);
                if (experiment == null) return false;

                repo.Remove(experiment);
                await _unitOfWork.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Configuration

        public async Task<ExperimentConfiguration?> GetConfigurationByIdAsync(int id)
        {
            var repo = _unitOfWork.GetRepository<ExperimentConfiguration>();
            return await repo.FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<bool> AddConfigurationAsync(ExperimentConfiguration configuration)
        {
            try
            {
                var repo = _unitOfWork.GetRepository<ExperimentConfiguration>();
                await repo.AddAsync(configuration);
                await _unitOfWork.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateConfigurationAsync(ExperimentConfiguration configuration)
        {
            try
            {
                var repo = _unitOfWork.GetRepository<ExperimentConfiguration>();
                repo.Update(configuration);
                await _unitOfWork.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteConfigurationAsync(int id)
        {
            try
            {
                var repo = _unitOfWork.GetRepository<ExperimentConfiguration>();
                var config = await repo.GetByIdAsync(id);
                if (config == null) return false;

                repo.Remove(config);
                await _unitOfWork.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region TestQuestion

        public async Task<TestQuestion?> GetTestQuestionByIdAsync(int id)
        {
            var repo = _unitOfWork.GetRepository<TestQuestion>();
            return await repo.FirstOrDefaultAsync(q => q.Id == id);
        }

        public async Task<bool> AddTestQuestionAsync(TestQuestion question)
        {
            try
            {
                question.CreatedAt = DateTime.UtcNow;
                var repo = _unitOfWork.GetRepository<TestQuestion>();
                await repo.AddAsync(question);
                await _unitOfWork.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateTestQuestionAsync(TestQuestion question)
        {
            try
            {
                var repo = _unitOfWork.GetRepository<TestQuestion>();
                repo.Update(question);
                await _unitOfWork.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteTestQuestionAsync(int id)
        {
            try
            {
                var repo = _unitOfWork.GetRepository<TestQuestion>();
                var question = await repo.GetByIdAsync(id);
                if (question == null) return false;

                repo.Remove(question);
                await _unitOfWork.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Run Experiment (Real RAG Pipeline)

        public async Task<(bool Success, string Message)> RunExperimentAsync(int experimentId)
        {
            var experiment = await GetExperimentWithDetailsAsync(experimentId);

            if (experiment == null)
            {
                return (false, "Experiment not found.");
            }

            if (!experiment.TestQuestions.Any())
            {
                return (false, "Cannot run experiment without test questions. Please add at least one test question.");
            }

            if (!experiment.Configurations.Any())
            {
                return (false, "Cannot run experiment without configurations. Please add at least one configuration.");
            }

            // Delete old results
            var resultRepo = _unitOfWork.GetRepository<ExperimentResult>();
            var oldResults = await resultRepo.FindAsync(r => r.ExperimentId == experimentId);
            if (oldResults.Any())
            {
                resultRepo.RemoveRange(oldResults);
                await _unitOfWork.SaveChangesAsync();
            }

            var docRepo = _unitOfWork.GetRepository<Document>();
            var documents = await docRepo.FindAsync(d => d.SubjectId == experiment.SubjectId && !string.IsNullOrEmpty(d.FilePath));
            var documentList = documents.ToList();

            var webRootPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot");
            var results = new List<ExperimentResult>();

            // Run for each configuration and question
            foreach (var config in experiment.Configurations)
            {
                var collectionName = $"exp_{experimentId}_config_{config.Id}";

                // Clean up any existing collection data in vector DB by deleting by document ID
                foreach (var doc in documentList)
                {
                    try
                    {
                        await _vectorDb.DeleteByDocumentAsync(collectionName, doc.Id);
                    }
                    catch { /* Ignore */ }
                }

                // Index documents for this configuration
                foreach (var doc in documentList)
                {
                    try
                    {
                        var fullPath = System.IO.Path.Combine(webRootPath, doc.FilePath);
                        if (!System.IO.File.Exists(fullPath)) continue;

                        var text = await _textExtractor.ExtractTextAsync(fullPath, doc.FileType);
                        if (string.IsNullOrWhiteSpace(text)) continue;

                        var chunks = _chunking.ChunkText(text, config.ChunkingStrategy, config.ChunkSize, config.ChunkOverlap);

                        for (int i = 0; i < chunks.Count; i++)
                        {
                            var chunkId = $"exp{experimentId}_config{config.Id}_doc{doc.Id}_chunk{i}";
                            var metadata = new Dictionary<string, string>
                            {
                                ["document_id"] = doc.Id.ToString(),
                                ["chunk_index"] = i.ToString(),
                                ["subject_id"]  = doc.SubjectId.ToString()
                            };

                            var embedding = await _embeddingService.GetEmbeddingAsync(chunks[i], config.EmbeddingModel);
                            await _vectorDb.UpsertAsync(collectionName, chunkId, embedding, chunks[i], metadata);
                        }
                    }
                    catch
                    {
                        // Log and continue
                    }
                }

                // Now execute the pipeline for each question
                foreach (var question in experiment.TestQuestions.OrderBy(q => q.OrderIndex))
                {
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    var matchedChunks = new List<VectorSearchResult>();

                    try
                    {
                        var queryEmbedding = await _embeddingService.GetEmbeddingAsync(question.Question, config.EmbeddingModel);
                        var chromaResults = await _vectorDb.QueryAsync(collectionName, queryEmbedding, config.TopK);

                        if (chromaResults != null && chromaResults.Count > 0)
                        {
                            matchedChunks = chromaResults.Where(r => r.Score >= config.SimilarityThreshold).ToList();
                            // Fallback to top 1 if threshold filters everything but we had results
                            if (matchedChunks.Count == 0 && chromaResults.Count > 0)
                            {
                                matchedChunks.Add(chromaResults.OrderByDescending(r => r.Score).First());
                            }
                        }
                    }
                    catch
                    {
                        // Retrieve fallback
                    }

                    // Fallback to database keyword search if no contexts retrieved from vector DB
                    if (matchedChunks.Count == 0)
                    {
                        try
                        {
                            var allChunks = await _unitOfWork.GetRepository<DocumentChunk>().FindAsync(
                                c => c.Document.SubjectId == experiment.SubjectId,
                                "Document"
                            );

                            var queryWords = question.Question.Split(' ', System.StringSplitOptions.RemoveEmptyEntries)
                                                             .Where(w => w.Length > 3)
                                                             .Take(5)
                                                             .ToList();

                            var fallbackResults = allChunks
                                .Select(c => new VectorSearchResult
                                {
                                    Id = c.EmbeddingId ?? "",
                                    Score = queryWords.Count > 0
                                        ? (double)queryWords.Count(w => c.Content.Contains(w, System.StringComparison.OrdinalIgnoreCase)) / queryWords.Count
                                        : (c.Content.Contains(question.Question, System.StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.0),
                                    Document = c.Content
                                })
                                .Where(x => x.Score > 0)
                                .OrderByDescending(x => x.Score)
                                .Take(config.TopK)
                                .ToList();

                            matchedChunks.AddRange(fallbackResults);
                        }
                        catch { /* Ignore */ }
                    }

                    var contextList = matchedChunks.Select(m => m.Document).Where(d => !string.IsNullOrEmpty(d)).ToList();
                    var contextText = string.Join("\n\n", contextList.Select((c, idx) => $"[Source {idx + 1}]: {c}"));

                    string systemPrompt = question.Question;
                    if (contextList.Count > 0)
                    {
                        systemPrompt = $"Context from uploaded documents:\n\n{contextText}\n\n" +
                                       $"Question: {question.Question}\n\n" +
                                       $"Please answer the question based on the provided context. Cite the context sources using bracketed numbers like [1], [2], etc., matching the Source numbers above.";
                    }

                    string generatedAnswer = "";
                    try
                    {
                        generatedAnswer = await _aiService.GenerateContentAsync(systemPrompt);
                    }
                    catch (Exception ex)
                    {
                        generatedAnswer = $"Failed to generate answer via LLM. Error: {ex.Message}";
                    }

                    stopwatch.Stop();
                    var latencyMs = (int)stopwatch.ElapsedMilliseconds;

                    // Evaluate RAGAS metrics
                    var metrics = await EvaluateRagasMetricsAsync(question.Question, question.GroundTruth, generatedAnswer, contextList);

                    var result = new ExperimentResult
                    {
                        ExperimentId = experimentId,
                        ConfigId = config.Id,
                        QuestionId = question.Id,
                        GeneratedAnswer = generatedAnswer,
                        RetrievedContexts = System.Text.Json.JsonSerializer.Serialize(contextList),
                        ContextPrecision = (float)Math.Round(metrics.Precision, 4),
                        ContextRecall = (float)Math.Round(metrics.Recall, 4),
                        Faithfulness = (float)Math.Round(metrics.Faithfulness, 4),
                        AnswerRelevancy = (float)Math.Round(metrics.Relevancy, 4),
                        RAGASScore = (float)Math.Round((metrics.Precision + metrics.Recall + metrics.Faithfulness + metrics.Relevancy) / 4f, 4),
                        LatencyMs = latencyMs,
                        CreatedAt = DateTime.UtcNow
                    };

                    results.Add(result);
                }

                // Clean up collection chunks from vector DB
                foreach (var doc in documentList)
                {
                    try
                    {
                        await _vectorDb.DeleteByDocumentAsync(collectionName, doc.Id);
                    }
                    catch { /* Ignore */ }
                }
            }

            // Save results to DB
            await resultRepo.AddRangeAsync(results);
            await _unitOfWork.SaveChangesAsync();

            // Update experiment status
            experiment.Status = ExperimentStatus.Completed;
            experiment.CompletedAt = DateTime.UtcNow;
            var expRepo = _unitOfWork.GetRepository<Experiment>();
            expRepo.Update(experiment);
            await _unitOfWork.SaveChangesAsync();

            return (true, $"Experiment completed successfully. Generated {results.Count} results.");
        }

        private async Task<RagasMetrics> EvaluateRagasMetricsAsync(string question, string groundTruth, string generatedAnswer, List<string> contexts)
        {
            var metrics = new RagasMetrics();

            if (contexts == null || contexts.Count == 0)
            {
                metrics.Precision = 0f;
                metrics.Recall = 0f;
                metrics.Faithfulness = 0f;
                metrics.Relevancy = CalculateJaccardSimilarity(generatedAnswer, question);
                return metrics;
            }

            // 1. Context Precision
            try
            {
                var contextTextList = string.Join("\n\n", contexts.Select((c, idx) => $"Context {idx}: {c}"));
                var prompt = $"Given the question and the retrieved contexts, analyze how relevant each retrieved context is for answering the question.\n" +
                             $"Question: {question}\n" +
                             $"Retrieved Contexts:\n{contextTextList}\n\n" +
                             $"Determine if each context is relevant (true) or not relevant (false) to the question.\n" +
                             $"Output your response in the following JSON format:\n" +
                             $"{{\n  \"relevance\": [true, false, ...]\n}}\n" +
                             $"Do not include any explanation, markdown formatting, or text outside the JSON.";

                var response = await _aiService.GenerateContentAsync(prompt);
                var cleaned = CleanJsonResponse(response);
                var res = System.Text.Json.JsonSerializer.Deserialize<ContextPrecisionResponse>(cleaned);
                if (res?.Relevance != null && res.Relevance.Length > 0)
                {
                    metrics.Precision = (float)res.Relevance.Count(r => r) / res.Relevance.Length;
                }
                else
                {
                    metrics.Precision = CalculateJaccardSimilarity(string.Join(" ", contexts), question);
                }
            }
            catch
            {
                metrics.Precision = CalculateJaccardSimilarity(string.Join(" ", contexts), question);
            }

            // 2. Context Recall
            try
            {
                var contextText = string.Join("\n\n", contexts);
                var prompt = $"Given the ground truth answer and the retrieved contexts, determine if the statements in the ground truth answer can be found in the retrieved contexts.\n" +
                             $"Ground Truth: {groundTruth}\n" +
                             $"Retrieved Contexts:\n{contextText}\n\n" +
                             $"Break the ground truth answer down into key statements. For each statement, determine if it can be attributed to the retrieved contexts (true) or not (false).\n" +
                             $"Output your response in the following JSON format:\n" +
                             $"{{\n  \"statements\": [\n    {{ \"statement\": \"statement text\", \"attributed\": true }},\n    ...\n  ]\n}}\n" +
                             $"Do not include any explanation, markdown formatting, or text outside the JSON.";

                var response = await _aiService.GenerateContentAsync(prompt);
                var cleaned = CleanJsonResponse(response);
                var res = System.Text.Json.JsonSerializer.Deserialize<ContextRecallResponse>(cleaned);
                if (res?.Statements != null && res.Statements.Length > 0)
                {
                    metrics.Recall = (float)res.Statements.Count(s => s.Attributed) / res.Statements.Length;
                }
                else
                {
                    metrics.Recall = CalculateJaccardSimilarity(string.Join(" ", contexts), groundTruth);
                }
            }
            catch
            {
                metrics.Recall = CalculateJaccardSimilarity(string.Join(" ", contexts), groundTruth);
            }

            // 3. Faithfulness
            try
            {
                var contextText = string.Join("\n\n", contexts);
                var prompt = $"Given the generated answer and the retrieved contexts, determine if the claims made in the generated answer are fully supported by the retrieved contexts.\n" +
                             $"Generated Answer: {generatedAnswer}\n" +
                             $"Retrieved Contexts:\n{contextText}\n\n" +
                             $"Break the generated answer down into key claims. For each claim, check if it is directly supported by the retrieved contexts (true) or not (false).\n" +
                             $"Output your response in the following JSON format:\n" +
                             $"{{\n  \"claims\": [\n    {{ \"claim\": \"claim text\", \"supported\": true }},\n    ...\n  ]\n}}\n" +
                             $"Do not include any explanation, markdown formatting, or text outside the JSON.";

                var response = await _aiService.GenerateContentAsync(prompt);
                var cleaned = CleanJsonResponse(response);
                var res = System.Text.Json.JsonSerializer.Deserialize<FaithfulnessResponse>(cleaned);
                if (res?.Claims != null && res.Claims.Length > 0)
                {
                    metrics.Faithfulness = (float)res.Claims.Count(c => c.Supported) / res.Claims.Length;
                }
                else
                {
                    metrics.Faithfulness = CalculateJaccardSimilarity(generatedAnswer, string.Join(" ", contexts));
                }
            }
            catch
            {
                metrics.Faithfulness = CalculateJaccardSimilarity(generatedAnswer, string.Join(" ", contexts));
            }

            // 4. Answer Relevancy
            try
            {
                var prompt = $"Evaluate the relevancy of the generated answer to the question.\n" +
                             $"Question: {question}\n" +
                             $"Generated Answer: {generatedAnswer}\n\n" +
                             $"Output a single JSON object containing a float score between 0.0 and 1.0 representing how relevant the answer is to the question (where 1.0 is highly relevant and 0.0 is completely irrelevant or off-topic).\n" +
                             $"Format:\n" +
                             $"{{\n  \"score\": 0.85\n}}\n" +
                             $"Do not include any explanation, markdown formatting, or text outside the JSON.";

                var response = await _aiService.GenerateContentAsync(prompt);
                var cleaned = CleanJsonResponse(response);
                var res = System.Text.Json.JsonSerializer.Deserialize<AnswerRelevancyResponse>(cleaned);
                if (res != null)
                {
                    metrics.Relevancy = res.Score;
                }
                else
                {
                    metrics.Relevancy = CalculateJaccardSimilarity(generatedAnswer, question);
                }
            }
            catch
            {
                metrics.Relevancy = CalculateJaccardSimilarity(generatedAnswer, question);
            }

            // Ensure values are bounded between 0 and 1
            metrics.Precision = Math.Clamp(metrics.Precision, 0f, 1f);
            metrics.Recall = Math.Clamp(metrics.Recall, 0f, 1f);
            metrics.Faithfulness = Math.Clamp(metrics.Faithfulness, 0f, 1f);
            metrics.Relevancy = Math.Clamp(metrics.Relevancy, 0f, 1f);

            return metrics;
        }

        private string CleanJsonResponse(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "{}";
            
            // Extract content from markdown JSON block if present
            var match = System.Text.RegularExpressions.Regex.Match(raw, @"```json\s*(.*?)\s*```", System.Text.RegularExpressions.RegexOptions.Singleline);
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }

            // Otherwise find the first '{' and last '}'
            int firstBrace = raw.IndexOf('{');
            int lastBrace = raw.LastIndexOf('}');
            if (firstBrace >= 0 && lastBrace > firstBrace)
            {
                return raw.Substring(firstBrace, lastBrace - firstBrace + 1).Trim();
            }

            return raw.Trim();
        }

        private float CalculateJaccardSimilarity(string s1, string s2)
        {
            if (string.IsNullOrWhiteSpace(s1) || string.IsNullOrWhiteSpace(s2)) return 0f;

            var words1 = s1.ToLower().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                           .Where(w => w.Length > 2)
                           .ToHashSet();
            var words2 = s2.ToLower().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                           .Where(w => w.Length > 2)
                           .ToHashSet();

            if (words1.Count == 0 || words2.Count == 0) return 0f;

            int intersection = words1.Intersect(words2).Count();
            int union = words1.Union(words2).Count();

            return (float)intersection / union;
        }

        private class RagasMetrics
        {
            public float Precision { get; set; } = 0.0f;
            public float Recall { get; set; } = 0.0f;
            public float Faithfulness { get; set; } = 0.0f;
            public float Relevancy { get; set; } = 0.0f;
        }

        private class ContextPrecisionResponse
        {
            [System.Text.Json.Serialization.JsonPropertyName("relevance")]
            public bool[]? Relevance { get; set; }
        }

        private class ContextRecallResponse
        {
            [System.Text.Json.Serialization.JsonPropertyName("statements")]
            public RecallStatement[]? Statements { get; set; }
        }

        private class RecallStatement
        {
            [System.Text.Json.Serialization.JsonPropertyName("statement")]
            public string Statement { get; set; } = "";

            [System.Text.Json.Serialization.JsonPropertyName("attributed")]
            public bool Attributed { get; set; }
        }

        private class FaithfulnessResponse
        {
            [System.Text.Json.Serialization.JsonPropertyName("claims")]
            public FaithfulnessClaim[]? Claims { get; set; }
        }

        private class FaithfulnessClaim
        {
            [System.Text.Json.Serialization.JsonPropertyName("claim")]
            public string Claim { get; set; } = "";

            [System.Text.Json.Serialization.JsonPropertyName("supported")]
            public bool Supported { get; set; }
        }

        private class AnswerRelevancyResponse
        {
            [System.Text.Json.Serialization.JsonPropertyName("score")]
            public float Score { get; set; }
        }

        #endregion

        #region Dashboard

        public async Task<ExperimentDashboardData> GetDashboardDataAsync(int experimentId)
        {
            var experiment = await GetExperimentWithDetailsAsync(experimentId);
            if (experiment == null)
            {
                return new ExperimentDashboardData();
            }

            var resultRepo = _unitOfWork.GetRepository<ExperimentResult>();
            var results = await resultRepo.FindAsync(
                r => r.ExperimentId == experimentId,
                "Configuration",
                "Question"
            );

            var resultsList = results.ToList();

            var data = new ExperimentDashboardData
            {
                Experiment = experiment,
                TotalQuestions = experiment.TestQuestions.Count,
                TotalConfigurations = experiment.Configurations.Count,
                AverageRAGASScore = resultsList.Any()
                    ? Math.Round(resultsList.Average(r => r.RAGASScore ?? 0), 4)
                    : 0,
                AverageLatencyMs = resultsList.Any()
                    ? Math.Round(resultsList.Average(r => r.LatencyMs), 2)
                    : 0,
                AllResults = resultsList
            };

            // Calculate metrics per configuration
            if (resultsList.Any())
            {
                var configGroups = resultsList.GroupBy(r => r.Configuration?.ConfigName ?? "Unknown");

                foreach (var group in configGroups)
                {
                    data.ConfigurationMetrics.Add(new ConfigurationMetric
                    {
                        ConfigName = group.Key,
                        AverageRAGASScore = Math.Round(group.Average(r => r.RAGASScore ?? 0), 4),
                        AverageLatencyMs = Math.Round(group.Average(r => r.LatencyMs), 2)
                    });
                }
            }

            return data;
        }

        #endregion
    }
}
