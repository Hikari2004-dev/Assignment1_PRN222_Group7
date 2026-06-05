using Assignment1_PRN222_Group7_DAL.Entities;
using Assignment1_PRN222_Group7_DAL.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Assignment1_PRN222_Group7_BLL.Services
{
    public class DocumentIndexingService : IDocumentIndexingService
    {
        private readonly IUnitOfWork _uow;
        private readonly ITextExtractorService _textExtractor;
        private readonly IChunkingService _chunking;
        private readonly IVectorDbService _vectorDb;
        private readonly IEmbeddingService _embedding;
        private readonly ILogger<DocumentIndexingService> _logger;
        private readonly string _collectionName;
        private readonly int _chunkSize;
        private readonly int _overlap;

        public DocumentIndexingService(
            IUnitOfWork uow,
            ITextExtractorService textExtractor,
            IChunkingService chunking,
            IVectorDbService vectorDb,
            IEmbeddingService embedding,
            IConfiguration config,
            ILogger<DocumentIndexingService> logger)
        {
            _uow           = uow;
            _textExtractor  = textExtractor;
            _chunking       = chunking;
            _vectorDb       = vectorDb;
            _embedding      = embedding;
            _logger         = logger;
            _collectionName = "hikari_docs";
            _chunkSize      = int.TryParse(config["Chunking:ChunkSize"], out var cs) ? cs : 500;
            _overlap        = int.TryParse(config["Chunking:Overlap"], out var ov) ? ov : 50;
        }

        public async Task IndexDocumentAsync(int documentId, string webRootPath)
        {
            var docRepo   = _uow.GetRepository<Document>();
            var chunkRepo = _uow.GetRepository<DocumentChunk>();

            var doc = await docRepo.GetByIdAsync(documentId);
            if (doc == null)
            {
                _logger.LogWarning("IndexDocumentAsync: Document {Id} not found", documentId);
                return;
            }

            try
            {
                // 1. Extract text
                var fullPath = Path.Combine(webRootPath, doc.FilePath);
                var text = await _textExtractor.ExtractTextAsync(fullPath, doc.FileType);
                if (string.IsNullOrWhiteSpace(text))
                {
                    _logger.LogWarning("IndexDocumentAsync: No text extracted from document {Id}", documentId);
                    return;
                }

                // 2. Chunk
                var chunks = _chunking.ChunkText(text, Assignment1_PRN222_Group7_DAL.Enums.ChunkingStrategy.Fixed, _chunkSize, _overlap);

                // 3. Remove old chunks (re-index support)
                var existingChunks = await chunkRepo.FindAsync(c => c.DocumentId == documentId);
                if (existingChunks.Any())
                {
                    // Also delete from vector DB
                    await _vectorDb.DeleteByDocumentAsync(_collectionName, documentId);
                    chunkRepo.RemoveRange(existingChunks);
                    await _uow.SaveChangesAsync();
                }

                // 4. Create new chunks + upsert to vector DB
                const string embeddingModel = "gemini-text-embedding-004";
                int indexed = 0;

                for (int i = 0; i < chunks.Count; i++)
                {
                    var chunkId = $"doc{documentId}_chunk{i}";
                    var metadata = new Dictionary<string, string>
                    {
                        ["document_id"] = documentId.ToString(),
                        ["chunk_index"] = i.ToString(),
                        ["subject_id"]  = doc.SubjectId.ToString()
                    };

                    var embedding = await _embedding.GetEmbeddingAsync(chunks[i], Assignment1_PRN222_Group7_DAL.Enums.EmbeddingModel.GeminiEmbedding004);
                    var embeddingId = await _vectorDb.UpsertAsync(_collectionName, chunkId, embedding, chunks[i], metadata);

                    var chunk = new DocumentChunk
                    {
                        DocumentId    = documentId,
                        ChunkIndex    = i,
                        Content       = chunks[i],
                        ContentLength = chunks[i].Length,
                        EmbeddingId   = embeddingId,
                        EmbeddingModel = embeddingId != null ? embeddingModel : null,
                        CreatedAt     = DateTime.UtcNow
                    };

                    await chunkRepo.AddAsync(chunk);
                    if (embeddingId != null) indexed++;
                }

                // 5. Update document status
                doc.TotalChunks       = chunks.Count;
                doc.IsIndexed         = indexed > 0;
                doc.IndexedAt         = indexed > 0 ? DateTime.UtcNow : null;
                doc.EmbeddingModelUsed = indexed > 0 ? embeddingModel : null;
                docRepo.Update(doc);
                await _uow.SaveChangesAsync();

                _logger.LogInformation(
                    "Indexed document {Id}: {Total} chunks, {Indexed} embedded",
                    documentId, chunks.Count, indexed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to index document {Id}", documentId);
            }
        }
    }
}
