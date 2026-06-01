-- ============================================================
-- Database: rag_learning_assistant (hoặc ChatbotPRN222_DB)
-- Dự án: Assignment1_PRN222_Group7
-- Mô tả: Chatbot hỏi đáp tài liệu môn học (RAG + Fine-tuning)
-- Auth:  Cookie Authentication đơn giản (không dùng ASP.NET Identity)
-- Kiến trúc: 3-Layer (DAL/BLL/Web) + MVC
-- Tạo ngày: 2026-06-01
--
-- CÁCH DÙNG:
--   Local:  sqlcmd -S "(localdb)\mssqllocaldb" -d ChatbotPRN222_DB -i CreateDatabase.sql
--   Remote: sqlcmd -S host,port -U user -P pass -d rag_learning_assistant -i CreateDatabase.sql
--   Hoặc chạy thẳng trong SSMS sau khi chọn đúng database.
-- ============================================================

-- Script này chỉ CREATE TABLE IF NOT EXISTS và INSERT seed data.
-- Không tạo/xóa database — chạy được nhiều lần (idempotent).


-- ============================================================
-- PHẦN 1: ROLES + USERS (Cookie Auth)
-- ============================================================

-- Bảng Roles mô tả vai trò
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Roles')
CREATE TABLE [dbo].[Roles] (
    [Id]             INT           NOT NULL IDENTITY(1,1),
    [Name]           NVARCHAR(50)  NOT NULL,              -- "Student", "Lecturer", "Admin"
    [NormalizedName] NVARCHAR(50)  NOT NULL,              -- "STUDENT", "LECTURER", "ADMIN"
    [Description]    NVARCHAR(200) NULL,
    CONSTRAINT [PK_Roles] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_Roles_NormalizedName] UNIQUE ([NormalizedName])
);
GO

-- Seed 3 roles mặc định
IF NOT EXISTS (SELECT * FROM [Roles])
BEGIN
    SET IDENTITY_INSERT [Roles] ON;
    INSERT INTO [Roles] ([Id],[Name],[NormalizedName],[Description])
    VALUES
        (1, 'Student',  'STUDENT',  N'Sinh viên'),
        (2, 'Lecturer', 'LECTURER', N'Giảng viên'),
        (3, 'Admin',    'ADMIN',    N'Quản trị viên');
    SET IDENTITY_INSERT [Roles] OFF;
    PRINT 'Seeded Roles.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
CREATE TABLE [dbo].[Users] (
    [Id]               INT           NOT NULL IDENTITY(1,1),
    [FullName]         NVARCHAR(200) NOT NULL,
    [Email]            NVARCHAR(256) NOT NULL,
    [PasswordHash]     NVARCHAR(500) NOT NULL,   -- BCrypt hash
    [RoleId]           INT           NOT NULL,   -- FK → Roles.Id
    [StudentOrStaffId] NVARCHAR(50)  NULL,
    [IsActive]         BIT           NOT NULL DEFAULT 1,
    [CreatedAt]        DATETIME2(7)  NOT NULL DEFAULT GETUTCDATE(),
    [LastLoginAt]      DATETIME2(7)  NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_Users_Email] UNIQUE ([Email]),
    CONSTRAINT [FK_Users_Roles] FOREIGN KEY ([RoleId]) REFERENCES [Roles]([Id])
);
GO

-- ============================================================
-- PHẦN 2: SUBSCRIPTION PLANS (Free / Basic / Premium)
-- ============================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SubscriptionPlans')
CREATE TABLE [dbo].[SubscriptionPlans] (
    [Id]                    INT           NOT NULL IDENTITY(1,1),
    -- Tier: 0=Free, 1=Basic, 2=Premium
    [Tier]                  INT           NOT NULL,
    [Name]                  NVARCHAR(50)  NOT NULL,
    [Description]           NVARCHAR(500) NULL,
    [MaxDocumentsUpload]    INT           NOT NULL DEFAULT 5,   -- -1=unlimited
    [MaxChatsPerDay]        INT           NOT NULL DEFAULT 10,  -- -1=unlimited
    [MaxMessagesPerSession] INT           NOT NULL DEFAULT 20,  -- -1=unlimited
    [MaxSubjectsAccess]     INT           NOT NULL DEFAULT 1,   -- -1=unlimited
    [Price]                 DECIMAL(18,2) NOT NULL DEFAULT 0,
    [IsActive]              BIT           NOT NULL DEFAULT 1,
    CONSTRAINT [PK_SubscriptionPlans] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_SubscriptionPlans_Tier] UNIQUE ([Tier])
);
GO

IF NOT EXISTS (SELECT * FROM [SubscriptionPlans])
BEGIN
    SET IDENTITY_INSERT [SubscriptionPlans] ON;
    INSERT INTO [SubscriptionPlans] ([Id],[Tier],[Name],[Description],[MaxDocumentsUpload],[MaxChatsPerDay],[MaxMessagesPerSession],[MaxSubjectsAccess],[Price],[IsActive])
    VALUES
        (1, 0, N'Free',    N'Dùng thử miễn phí',                          5,   10,  20,  1,      0, 1),
        (2, 1, N'Basic',   N'Gói cơ bản cho sinh viên',                  50,  100,  50,  5,  99000, 1),
        (3, 2, N'Premium', N'Không giới hạn cho giảng viên & nghiên cứu',-1,   -1,  -1, -1, 299000, 1);
    SET IDENTITY_INSERT [SubscriptionPlans] OFF;
    PRINT 'Seeded SubscriptionPlans.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserSubscriptions')
CREATE TABLE [dbo].[UserSubscriptions] (
    [Id]            INT           NOT NULL IDENTITY(1,1),
    [UserId]        INT           NOT NULL,
    [PlanId]        INT           NOT NULL,
    [StartDate]     DATETIME2(7)  NOT NULL DEFAULT GETUTCDATE(),
    [EndDate]       DATETIME2(7)  NULL,
    [IsActive]      BIT           NOT NULL DEFAULT 1,
    -- PaymentStatus: 0=Pending, 1=Paid, 2=Cancelled, 3=Expired
    [PaymentStatus] INT           NOT NULL DEFAULT 0,
    [TransactionId] NVARCHAR(200) NULL,
    [CreatedAt]     DATETIME2(7)  NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [PK_UserSubscriptions] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_UserSubs_Users] FOREIGN KEY ([UserId]) REFERENCES [Users]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserSubs_Plans] FOREIGN KEY ([PlanId]) REFERENCES [SubscriptionPlans]([Id])
);
GO

-- ============================================================
-- PHẦN 3: DOCUMENT MANAGEMENT
-- ============================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Subjects')
CREATE TABLE [dbo].[Subjects] (
    [Id]          INT            NOT NULL IDENTITY(1,1),
    [Code]        NVARCHAR(20)   NOT NULL,
    [Name]        NVARCHAR(200)  NOT NULL,
    [Description] NVARCHAR(1000) NULL,
    [CreatedBy]   INT            NULL,   -- FK -> Users.Id (Lecturer)
    [CreatedAt]   DATETIME2(7)   NOT NULL DEFAULT GETUTCDATE(),
    [IsActive]    BIT            NOT NULL DEFAULT 1,
    CONSTRAINT [PK_Subjects] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_Subjects_Code] UNIQUE ([Code]),
    CONSTRAINT [FK_Subjects_Users] FOREIGN KEY ([CreatedBy]) REFERENCES [Users]([Id]) ON DELETE SET NULL
);
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Chapters')
CREATE TABLE [dbo].[Chapters] (
    [Id]          INT           NOT NULL IDENTITY(1,1),
    [SubjectId]   INT           NOT NULL,
    [Title]       NVARCHAR(300) NOT NULL,
    [Description] NVARCHAR(500) NULL,
    [OrderIndex]  INT           NOT NULL DEFAULT 1,
    [CreatedAt]   DATETIME2(7)  NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [PK_Chapters] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Chapters_Subjects] FOREIGN KEY ([SubjectId]) REFERENCES [Subjects]([Id]) ON DELETE CASCADE
);
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Documents')
CREATE TABLE [dbo].[Documents] (
    [Id]                 INT            NOT NULL IDENTITY(1,1),
    [Title]              NVARCHAR(500)  NOT NULL,
    [OriginalFileName]   NVARCHAR(500)  NOT NULL,
    [StoredFileName]     NVARCHAR(500)  NOT NULL,
    [FilePath]           NVARCHAR(1000) NOT NULL,
    -- FileType: 0=PDF, 1=DOCX, 2=PPTX, 3=TXT, 99=Other
    [FileType]           INT            NOT NULL DEFAULT 0,
    [FileSizeBytes]      BIGINT         NOT NULL DEFAULT 0,
    [SubjectId]          INT            NOT NULL,
    [ChapterId]          INT            NULL,
    [UploadedBy]         INT            NOT NULL,   -- FK -> Users.Id
    [UploadedAt]         DATETIME2(7)   NOT NULL DEFAULT GETUTCDATE(),
    [IsIndexed]          BIT            NOT NULL DEFAULT 0,
    [IndexedAt]          DATETIME2(7)   NULL,
    [TotalChunks]        INT            NOT NULL DEFAULT 0,
    [EmbeddingModelUsed] NVARCHAR(100)  NULL,
    [Description]        NVARCHAR(1000) NULL,
    CONSTRAINT [PK_Documents] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Documents_Subjects]  FOREIGN KEY ([SubjectId])  REFERENCES [Subjects]([Id]),
    CONSTRAINT [FK_Documents_Chapters]  FOREIGN KEY ([ChapterId])  REFERENCES [Chapters]([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Documents_Uploader]  FOREIGN KEY ([UploadedBy]) REFERENCES [Users]([Id])
);
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DocumentChunks')
CREATE TABLE [dbo].[DocumentChunks] (
    [Id]             INT           NOT NULL IDENTITY(1,1),
    [DocumentId]     INT           NOT NULL,
    [ChunkIndex]     INT           NOT NULL,
    [Content]        NVARCHAR(MAX) NOT NULL,
    [ContentLength]  INT           NOT NULL DEFAULT 0,
    [StartPage]      INT           NULL,
    [EndPage]        INT           NULL,
    [EmbeddingId]    NVARCHAR(200) NULL,   -- ID trong Chroma DB
    [EmbeddingModel] NVARCHAR(100) NULL,
    [CreatedAt]      DATETIME2(7)  NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [PK_DocumentChunks] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_DocumentChunks_Documents] FOREIGN KEY ([DocumentId]) REFERENCES [Documents]([Id]) ON DELETE CASCADE
);
GO

GO

-- ============================================================
-- PHẦN 4: CHAT & HỎI ĐÁP
-- ============================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ChatSessions')
CREATE TABLE [dbo].[ChatSessions] (
    [Id]            INT           NOT NULL IDENTITY(1,1),
    [UserId]        INT           NOT NULL,
    [SubjectId]     INT           NULL,
    [Title]         NVARCHAR(500) NOT NULL DEFAULT N'New Chat',
    [CreatedAt]     DATETIME2(7)  NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt]     DATETIME2(7)  NOT NULL DEFAULT GETUTCDATE(),
    [IsActive]      BIT           NOT NULL DEFAULT 1,
    [TotalMessages] INT           NOT NULL DEFAULT 0,
    CONSTRAINT [PK_ChatSessions] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_ChatSessions_Users]    FOREIGN KEY ([UserId])    REFERENCES [Users]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ChatSessions_Subjects] FOREIGN KEY ([SubjectId]) REFERENCES [Subjects]([Id]) ON DELETE SET NULL
);
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ChatMessages')
CREATE TABLE [dbo].[ChatMessages] (
    [Id]               INT           NOT NULL IDENTITY(1,1),
    [SessionId]        INT           NOT NULL,
    -- Role: 0=User, 1=Assistant, 2=System
    [Role]             INT           NOT NULL DEFAULT 0,
    [Content]          NVARCHAR(MAX) NOT NULL,
    [CreatedAt]        DATETIME2(7)  NOT NULL DEFAULT GETUTCDATE(),
    [TokensUsed]       INT           NULL,
    -- RetrievalMethod: 0=RAG, 1=FineTuned, 2=Hybrid
    [RetrievalMethod]  INT           NULL,
    [ProcessingTimeMs] INT           NULL,
    CONSTRAINT [PK_ChatMessages] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_ChatMessages_Sessions] FOREIGN KEY ([SessionId]) REFERENCES [ChatSessions]([Id]) ON DELETE CASCADE
);
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MessageSources')
CREATE TABLE [dbo].[MessageSources] (
    [Id]             INT           NOT NULL IDENTITY(1,1),
    [MessageId]      INT           NOT NULL,
    [ChunkId]        INT           NOT NULL,
    [SimilarityScore] REAL         NOT NULL DEFAULT 0,
    [CitedContent]   NVARCHAR(MAX) NULL,
    [SourceIndex]    INT           NOT NULL DEFAULT 0,
    CONSTRAINT [PK_MessageSources] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_MessageSources_Messages] FOREIGN KEY ([MessageId]) REFERENCES [ChatMessages]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_MessageSources_Chunks]   FOREIGN KEY ([ChunkId])   REFERENCES [DocumentChunks]([Id])
);
GO

-- ============================================================
-- PHẦN 5: MODULE NGHIÊN CỨU (RBL)
-- ============================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Experiments')
CREATE TABLE [dbo].[Experiments] (
    [Id]          INT            NOT NULL IDENTITY(1,1),
    [Name]        NVARCHAR(300)  NOT NULL,
    [Description] NVARCHAR(1000) NULL,
    [SubjectId]   INT            NOT NULL,
    [CreatedBy]   INT            NOT NULL,   -- FK -> Users.Id
    [CreatedAt]   DATETIME2(7)   NOT NULL DEFAULT GETUTCDATE(),
    [CompletedAt] DATETIME2(7)   NULL,
    -- Status: 0=Draft, 1=Running, 2=Completed, 3=Failed
    [Status]      INT            NOT NULL DEFAULT 0,
    CONSTRAINT [PK_Experiments] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Experiments_Subjects] FOREIGN KEY ([SubjectId]) REFERENCES [Subjects]([Id]),
    CONSTRAINT [FK_Experiments_Users]    FOREIGN KEY ([CreatedBy]) REFERENCES [Users]([Id])
);
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ExperimentConfigurations')
CREATE TABLE [dbo].[ExperimentConfigurations] (
    [Id]                  INT           NOT NULL IDENTITY(1,1),
    [ExperimentId]        INT           NOT NULL,
    [ConfigName]          NVARCHAR(200) NOT NULL,
    -- ChunkingStrategy: 0=Fixed, 1=Recursive, 2=Semantic, 3=Sentence
    [ChunkingStrategy]    INT           NOT NULL DEFAULT 0,
    [ChunkSize]           INT           NOT NULL DEFAULT 512,
    [ChunkOverlap]        INT           NOT NULL DEFAULT 64,
    -- EmbeddingModel: 0=MultilingualE5Base, 1=TextEmbedding3Small, 2=PhoBERT, 3=BgeM3
    [EmbeddingModel]      INT           NOT NULL DEFAULT 0,
    -- RetrievalMethod: 0=RAG, 1=FineTuned, 2=Hybrid
    [RetrievalMethod]     INT           NOT NULL DEFAULT 0,
    [TopK]                INT           NOT NULL DEFAULT 5,
    [SimilarityThreshold] REAL          NOT NULL DEFAULT 0.7,
    CONSTRAINT [PK_ExperimentConfigurations] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_ExpConfigs_Experiments] FOREIGN KEY ([ExperimentId]) REFERENCES [Experiments]([Id]) ON DELETE CASCADE
);
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TestQuestions')
CREATE TABLE [dbo].[TestQuestions] (
    [Id]               INT           NOT NULL IDENTITY(1,1),
    [ExperimentId]     INT           NOT NULL,
    [Question]         NVARCHAR(MAX) NOT NULL,
    [GroundTruth]      NVARCHAR(MAX) NOT NULL,
    [ReferenceContext] NVARCHAR(MAX) NULL,
    [OrderIndex]       INT           NOT NULL DEFAULT 0,
    [CreatedAt]        DATETIME2(7)  NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [PK_TestQuestions] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_TestQuestions_Experiments] FOREIGN KEY ([ExperimentId]) REFERENCES [Experiments]([Id]) ON DELETE CASCADE
);
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ExperimentResults')
CREATE TABLE [dbo].[ExperimentResults] (
    [Id]               INT           NOT NULL IDENTITY(1,1),
    [ExperimentId]     INT           NOT NULL,
    [ConfigId]         INT           NOT NULL,
    [QuestionId]       INT           NOT NULL,
    [GeneratedAnswer]  NVARCHAR(MAX) NULL,
    [RetrievedContexts] NVARCHAR(MAX) NULL,
    -- RAGAS Metrics (0.0 - 1.0)
    [ContextPrecision] REAL          NULL,
    [ContextRecall]    REAL          NULL,
    [Faithfulness]     REAL          NULL,
    [AnswerRelevancy]  REAL          NULL,
    [RAGASScore]       REAL          NULL,
    [LatencyMs]        INT           NOT NULL DEFAULT 0,
    [CreatedAt]        DATETIME2(7)  NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [PK_ExperimentResults] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_ExpResults_Experiments] FOREIGN KEY ([ExperimentId]) REFERENCES [Experiments]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ExpResults_Configs]     FOREIGN KEY ([ConfigId])     REFERENCES [ExperimentConfigurations]([Id]),
    CONSTRAINT [FK_ExpResults_Questions]   FOREIGN KEY ([QuestionId])   REFERENCES [TestQuestions]([Id])
);
GO

-- ============================================================
-- PHẦN 6: SEED DỮ LIỆU MẪU
-- ============================================================

-- Seed môn học demo
IF NOT EXISTS (SELECT * FROM [Subjects] WHERE [Code] = 'PRN222')
BEGIN
    INSERT INTO [Subjects] ([Code], [Name], [Description], [IsActive])
    VALUES ('PRN222', N'C# Programming with ASP.NET Core', N'Môn học lập trình C# với ASP.NET Core MVC', 1);
    PRINT 'Seeded Subject PRN222.';
END
GO

-- ============================================================
PRINT '====================================================';
PRINT 'Database ChatbotPRN222_DB đã sẵn sàng!';
PRINT 'Bảng đã tạo (13 bảng):';
PRINT '  [Users]            - Cookie Auth (int PK, BCrypt)';
PRINT '  [SubscriptionPlans, UserSubscriptions]';
PRINT '  [Subjects, Chapters, Documents, DocumentChunks]';
PRINT '  [ChatSessions, ChatMessages, MessageSources]';
PRINT '  [Experiments, ExperimentConfigurations, TestQuestions, ExperimentResults]';
PRINT '====================================================';
GO
