# Task 4.2 & 4.3 — Document Upload, Text Extraction & Vector Indexing

## Tổng quan

**Task 4.2**: Upload tài liệu (PDF/DOCX/PPTX/TXT) → lưu file → trích xuất text → lưu `Document` vào DB.  
**Task 4.3**: Chunking văn bản → tạo `DocumentChunk` → gọi embedding model → đẩy vector lên Chroma DB.

---

## Giả định rõ ràng

> [!IMPORTANT]
> - Chroma DB được gọi qua **HTTP REST API** (ChromaDB server chạy local, mặc định `http://localhost:8000`)
> - Embedding model: **multilingual-e5-base** gọi qua Python service local hoặc **sentence-transformers** API — hoặc stub fake vector (float array random) cho demo nếu chưa có server
> - Hỗ trợ file: PDF (dùng `PdfPig`), DOCX (dùng `DocumentFormat.OpenXml`), TXT
> - PPTX: trích xuất text đơn giản qua OpenXml
> - Chunking strategy: **Fixed (sliding window)** — `chunkSize=500 tokens`, `overlap=50`
> - Task 4.3 chạy **background** sau khi upload xong (không block UI)

> [!NOTE]
> Nếu Chroma server hoặc embedding service chưa chạy, hệ thống vẫn tạo chunk trong SQL DB nhưng `EmbeddingId = null`, `IsIndexed = false`.

---

## Proposed Changes

### Layer 1 — NuGet packages

#### [MODIFY] Assignment1_PRN222_Group7_BLL.csproj
- Thêm `PdfPig` (PDF text extraction)
- Thêm `DocumentFormat.OpenXml` (DOCX/PPTX text extraction)

#### [MODIFY] Assignment1_PRN222_Group7.csproj
- Không cần thêm package (HTTP client đã có sẵn)

---

### Layer 2 — BLL Services

#### [NEW] IDocumentService.cs
```
GetDocumentsBySubjectAsync(subjectId) → IEnumerable<Document>
GetDocumentByIdAsync(id) → Document?
UploadDocumentAsync(file, subjectId, chapterId?, title, userId) → Document
DeleteDocumentAsync(id) → bool
```

#### [NEW] DocumentService.cs
- Lưu file vào `wwwroot/uploads/documents/`
- Trích xuất text bằng `TextExtractorService`
- Tạo `Document` record trong DB
- Trigger background chunking (fire-and-forget hoặc queue)

#### [NEW] ITextExtractorService.cs + TextExtractorService.cs
- `ExtractTextAsync(filePath, fileType) → string`
- Switch theo `FileType`: PDF → PdfPig, DOCX/PPTX → OpenXml, TXT → File.ReadAll

#### [NEW] IChunkingService.cs + ChunkingService.cs
- `ChunkText(text, chunkSize, overlap) → List<string>`
- Strategy: Fixed sliding window

#### [NEW] IVectorDbService.cs + ChromaVectorDbService.cs
- `UpsertAsync(collectionName, id, text, metadata) → string embeddingId`
- Gọi HTTP đến Chroma REST API
- Nếu Chroma không khả dụng: return null (graceful degrade)

#### [NEW] IDocumentIndexingService.cs + DocumentIndexingService.cs
- `IndexDocumentAsync(documentId)` — orchestrate: chunk → embed → upsert Chroma → update DB

---

### Layer 3 — Web Layer

#### [NEW] DocumentController.cs
- `GET  /Subject/{subjectId}/Document` → list tài liệu theo môn
- `GET  /Subject/{subjectId}/Document/Upload` → form upload
- `POST /Subject/{subjectId}/Document/Upload` → xử lý upload
- `POST /Subject/{subjectId}/Document/Index/{id}` → trigger indexing thủ công
- `GET  /Subject/{subjectId}/Document/Delete/{id}` → confirm xóa
- `POST /Subject/{subjectId}/Document/Delete/{id}` → xóa file + DB

#### [NEW] Views/Document/
- `Index.cshtml` — danh sách document, hiện trạng indexing
- `Upload.cshtml` — form upload
- `Delete.cshtml` — confirm xóa

---

### Layer 4 — Configuration

#### [MODIFY] appsettings.json
```json
"DocumentStorage": { "BasePath": "uploads/documents" },
"ChromaDb": { "BaseUrl": "http://localhost:8000", "CollectionName": "hikari_docs" },
"Chunking": { "ChunkSize": 500, "Overlap": 50 }
```

#### [MODIFY] Program.cs
- Đăng ký: `ITextExtractorService`, `IChunkingService`, `IVectorDbService`, `IDocumentIndexingService`, `IDocumentService`
- `AddHttpClient<IVectorDbService, ChromaVectorDbService>`

---

## Verification Plan

### Automated
```
dotnet build — 0 errors
```

### Manual (sau khi chạy)
1. Login → vào Subject → Document → Upload 1 file PDF
2. File xuất hiện trong danh sách, `IsIndexed = false`
3. Click "Index" → background job chạy → `IsIndexed = true`, `TotalChunks > 0`
4. Nếu Chroma không chạy → vẫn thấy chunks trong DB, `EmbeddingId = null`
