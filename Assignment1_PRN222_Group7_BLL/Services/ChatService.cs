using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Assignment1_PRN222_Group7_DAL.Entities;
using Assignment1_PRN222_Group7_DAL.Enums;
using Assignment1_PRN222_Group7_DAL.Repositories;
using Microsoft.Extensions.Logging;

namespace Assignment1_PRN222_Group7_BLL.Services
{
    public class ChatService : IChatService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IVectorDbService _vectorDb;
        private readonly IAiService _aiService;
        private readonly ILogger<ChatService> _logger;
        private const string CollectionName = "hikari_docs";

        public ChatService(
            IUnitOfWork unitOfWork,
            IVectorDbService vectorDb,
            IAiService aiService,
            ILogger<ChatService> logger)
        {
            _unitOfWork = unitOfWork;
            _vectorDb = vectorDb;
            _aiService = aiService;
            _logger = logger;
        }

        public async Task<ChatSession> CreateSessionAsync(int userId, int? subjectId, string title = "New Chat")
        {
            var session = new ChatSession
            {
                UserId = userId,
                SubjectId = subjectId,
                Title = title,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true,
                TotalMessages = 0
            };

            await _unitOfWork.GetRepository<ChatSession>().AddAsync(session);
            await _unitOfWork.SaveChangesAsync();
            return session;
        }

        public async Task<ChatSession?> GetSessionWithMessagesAsync(int sessionId)
        {
            // Use FirstOrDefaultAsync with the correct include string path
            return await _unitOfWork.GetRepository<ChatSession>().FirstOrDefaultAsync(
                s => s.Id == sessionId,
                "Messages.Sources.Chunk.Document"
            );
        }

        public async Task<IEnumerable<ChatSession>> GetUserSessionsAsync(int userId)
        {
            // Get all active sessions for the user, ordered by last update
            var sessions = await _unitOfWork.GetRepository<ChatSession>().FindAsync(
                s => s.UserId == userId && s.IsActive,
                "Subject"
            );
            return sessions.OrderByDescending(s => s.UpdatedAt);
        }

        public async Task<bool> DeleteSessionAsync(int sessionId)
        {
            var repo = _unitOfWork.GetRepository<ChatSession>();
            var session = await repo.GetByIdAsync(sessionId);
            if (session == null) return false;

            session.IsActive = false; // Soft delete
            session.UpdatedAt = DateTime.UtcNow;
            repo.Update(session);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<ChatMessage> SendMessageAsync(int sessionId, string userContent)
        {
            var stopwatch = Stopwatch.StartNew();

            // 1. Fetch Session
            var session = await _unitOfWork.GetRepository<ChatSession>().FirstOrDefaultAsync(s => s.Id == sessionId, "Messages");
            if (session == null)
            {
                throw new KeyNotFoundException($"Chat session {sessionId} not found.");
            }

            // 2. Add User Message
            var userMsg = new ChatMessage
            {
                SessionId = sessionId,
                Role = MessageRole.User,
                Content = userContent,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.GetRepository<ChatMessage>().AddAsync(userMsg);

            // Update session info
            session.TotalMessages += 1;
            if (session.Title == "New Chat" || string.IsNullOrWhiteSpace(session.Title))
            {
                session.Title = userContent.Length > 40 ? userContent.Substring(0, 37) + "..." : userContent;
            }
            session.UpdatedAt = DateTime.UtcNow;

            // 3. Search Context (RAG)
            var matchedChunks = new List<(DocumentChunk Chunk, double Score)>();

            try
            {
                // Generate query embedding (768 dimensions)
                var queryEmbedding = GenerateStubEmbedding(768);
                var chromaResults = await _vectorDb.QueryAsync(CollectionName, queryEmbedding, 10);

                if (chromaResults != null && chromaResults.Count > 0)
                {
                    var chunkRepo = _unitOfWork.GetRepository<DocumentChunk>();
                    foreach (var result in chromaResults)
                    {
                        var chunk = await chunkRepo.FirstOrDefaultAsync(c => c.EmbeddingId == result.Id, "Document");
                        if (chunk != null)
                        {
                            // Filter by subject if specified in the session
                            if (session.SubjectId == null || chunk.Document.SubjectId == session.SubjectId)
                            {
                                matchedChunks.Add((chunk, result.Score));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve from Chroma DB. Falling back to DB keyword search.");
            }

            // Fallback: If no vector matches found, use keyword search on DocumentChunks
            if (matchedChunks.Count == 0)
            {
                var queryWords = userContent.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                            .Where(w => w.Length > 3)
                                            .Take(5)
                                            .ToList();

                var allChunks = await _unitOfWork.GetRepository<DocumentChunk>().FindAsync(
                    c => session.SubjectId == null || c.Document.SubjectId == session.SubjectId,
                    "Document"
                );

                var fallbackResults = allChunks
                    .Select(c => new
                    {
                        Chunk = c,
                        Score = queryWords.Count > 0
                            ? (double)queryWords.Count(w => c.Content.Contains(w, StringComparison.OrdinalIgnoreCase)) / queryWords.Count
                            : (c.Content.Contains(userContent, StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.0)
                    })
                    .Where(x => x.Score > 0)
                    .OrderByDescending(x => x.Score)
                    .Take(5)
                    .ToList();

                foreach (var fallback in fallbackResults)
                {
                    matchedChunks.Add((fallback.Chunk, fallback.Score));
                }
            }

            // Keep top 3 most relevant context sources
            var finalSources = matchedChunks.OrderByDescending(x => x.Score).Take(3).ToList();

            // 4. Construct System Prompt with Context
            var systemPrompt = userContent;
            if (finalSources.Count > 0)
            {
                var contextText = string.Join("\n\n", finalSources.Select((x, index) =>
                    $"[Source {index + 1}] Document: {x.Chunk.Document.Title} (Chunk #{x.Chunk.ChunkIndex + 1})\nContent: {x.Chunk.Content}"));

                systemPrompt = $"Context from uploaded documents:\n\n{contextText}\n\n" +
                               $"Question: {userContent}\n\n" +
                               $"Please answer the question based on the provided context. If the context does not contain enough information, use your general knowledge but clearly state that you are doing so. Cite the context sources using bracketed numbers like [1], [2], etc., matching the Source numbers above.";
            }

            // 5. Generate content using LLM
            var history = session.Messages.Where(m => m.Id != userMsg.Id).OrderBy(m => m.CreatedAt).ToList();
            var responseContent = await _aiService.GenerateContentAsync(systemPrompt, history);

            // 6. Save Assistant Message
            var assistantMsg = new ChatMessage
            {
                SessionId = sessionId,
                Role = MessageRole.Assistant,
                Content = responseContent,
                CreatedAt = DateTime.UtcNow,
                RetrievalMethod = RetrievalMethod.RAG,
                ProcessingTimeMs = (int)stopwatch.ElapsedMilliseconds,
                TokensUsed = responseContent.Length / 4 // Approximate
            };
            await _unitOfWork.GetRepository<ChatMessage>().AddAsync(assistantMsg);

            // Update session message count
            session.TotalMessages += 1;
            session.UpdatedAt = DateTime.UtcNow;

            // Save references to sources used
            int sourceIndex = 1;
            foreach (var source in finalSources)
            {
                var msgSource = new MessageSource
                {
                    Message = assistantMsg,
                    ChunkId = source.Chunk.Id,
                    SimilarityScore = (float)source.Score,
                    CitedContent = source.Chunk.Content,
                    SourceIndex = sourceIndex++
                };
                await _unitOfWork.GetRepository<MessageSource>().AddAsync(msgSource);
            }

            await _unitOfWork.SaveChangesAsync();
            return assistantMsg;
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
