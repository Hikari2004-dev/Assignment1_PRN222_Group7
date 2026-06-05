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
        private readonly IEmbeddingService _embeddingService;
        private readonly ILogger<ChatService> _logger;
        private const string CollectionName = "hikari_docs";

        public ChatService(
            IUnitOfWork unitOfWork,
            IVectorDbService vectorDb,
            IAiService aiService,
            IEmbeddingService embeddingService,
            ILogger<ChatService> logger)
        {
            _unitOfWork = unitOfWork;
            _vectorDb = vectorDb;
            _aiService = aiService;
            _embeddingService = embeddingService;
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
            const double MinScoreThreshold = 0.3;

            try
            {
                var queryEmbedding = await _embeddingService.GetEmbeddingAsync(userContent, EmbeddingModel.GeminiEmbedding004);
                var chromaResults = await _vectorDb.QueryAsync(CollectionName, queryEmbedding, 10);

                if (chromaResults != null && chromaResults.Count > 0)
                {
                    var chunkRepo = _unitOfWork.GetRepository<DocumentChunk>();
                    var seenDocIds = new HashSet<int>();

                    foreach (var result in chromaResults)
                    {
                        if (result.Score < MinScoreThreshold) continue;

                        var chunk = await chunkRepo.FirstOrDefaultAsync(c => c.EmbeddingId == result.Id, "Document");
                        if (chunk == null) continue;
                        if (session.SubjectId != null && chunk.Document.SubjectId != session.SubjectId) continue;
                        if (seenDocIds.Contains(chunk.DocumentId)) continue;
                        seenDocIds.Add(chunk.DocumentId);

                        matchedChunks.Add((chunk, result.Score));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve from Chroma DB. Falling back to keyword search.");
            }

            // Keyword fallback — only if vector search found nothing above threshold
            if (matchedChunks.Count == 0)
            {
                var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "a", "an", "the", "is", "are", "was", "were", "be", "been", "being",
                    "have", "has", "had", "do", "does", "did", "will", "would", "could", "should",
                    "may", "might", "must", "shall", "can", "need", "dare", "ought", "used",
                    "to", "of", "in", "for", "on", "with", "at", "by", "from", "as", "into",
                    "through", "during", "before", "after", "above", "below", "between", "under",
                    "again", "further", "then", "once", "here", "there", "when", "where", "why",
                    "how", "all", "each", "few", "more", "most", "other", "some", "such",
                    "no", "nor", "not", "only", "own", "same", "so", "than", "too", "very",
                    "just", "and", "but", "or", "if", "because", "until", "while", "this", "that",
                    "these", "those", "what", "which", "who", "whom", "its", "it", "he", "she",
                    "they", "them", "his", "her", "their", "my", "your", "our", "about", "out",
                    "net", "core"
                };

                var queryWords = userContent.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Where(w => w.Length > 2 && !stopWords.Contains(w))
                    .Take(10)
                    .ToList();

                if (queryWords.Count > 0)
                {
                    var allChunks = await _unitOfWork.GetRepository<DocumentChunk>().FindAsync(
                        c => session.SubjectId == null || c.Document.SubjectId == session.SubjectId,
                        "Document"
                    );

                    var seenDocIds = new HashSet<int>();
                    foreach (var chunk in allChunks)
                    {
                        if (seenDocIds.Contains(chunk.DocumentId)) continue;

                        var wordMatchCount = queryWords.Count(w =>
                            chunk.Content.Contains(w, StringComparison.OrdinalIgnoreCase));
                        var keywordScore = (double)wordMatchCount / queryWords.Count;

                        if (keywordScore >= 0.3)
                        {
                            seenDocIds.Add(chunk.DocumentId);
                            matchedChunks.Add((chunk, keywordScore));
                        }

                        if (matchedChunks.Count >= 5) break;
                    }
                }
            }

            // No relevant context found — reply with a clear message
            if (matchedChunks.Count == 0)
            {
                var noContextMsg = new ChatMessage
                {
                    SessionId = sessionId,
                    Role = MessageRole.Assistant,
                    Content = "I couldn't find any relevant documents to answer your question. Please make sure the course materials have been uploaded and indexed first, or try rephrasing your question using terms from the uploaded documents.",
                    CreatedAt = DateTime.UtcNow,
                    RetrievalMethod = RetrievalMethod.RAG,
                    ProcessingTimeMs = (int)stopwatch.ElapsedMilliseconds,
                    TokensUsed = 0
                };
                await _unitOfWork.GetRepository<ChatMessage>().AddAsync(noContextMsg);
                await _unitOfWork.SaveChangesAsync();
                return noContextMsg;
            }

            // Deduplicate: keep best-scoring chunk per document
            var topChunks = matchedChunks
                .GroupBy(x => x.Chunk.DocumentId)
                .Select(g => g.OrderByDescending(x => x.Score).First())
                .OrderByDescending(x => x.Score)
                .Take(3)
                .ToList();

            // 4. Construct System Prompt with Context
            var contextText = string.Join("\n\n", topChunks.Select((x, index) =>
                $"[Source {index + 1}] Document: {x.Chunk.Document.Title} (Chunk #{x.Chunk.ChunkIndex + 1})\nContent: {x.Chunk.Content}"));

            var systemPrompt = $"You are an educational AI assistant for a university learning platform. Your task is to answer questions ONLY using the provided context from uploaded documents.\n\nCRITICAL RULES:\n1. Answer ONLY based on the provided context. Do NOT use any external knowledge.\n2. If the context does not contain enough information to answer the question, you MUST respond with: \"I cannot answer this question based on the provided documents.\"\n3. You must NEVER make up information, add examples, or provide details not found in the context.\n4. If you are unsure or the question is outside the scope of the documents, say so clearly.\n\nContext from uploaded documents:\n\n{contextText}\n\nQuestion: {userContent}\n\nAnswer the question following the rules above. Cite sources using bracketed numbers like [1], [2], etc. matching the Source numbers above.";

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
            foreach (var source in topChunks)
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


    }
}
