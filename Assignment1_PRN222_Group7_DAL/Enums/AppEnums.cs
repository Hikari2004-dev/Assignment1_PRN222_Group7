namespace Assignment1_PRN222_Group7_DAL.Enums
{
    public enum UserRole
    {
        Student = 0,
        Lecturer = 1,
        Admin = 2
    }

    public enum SubscriptionTier
    {
        Free = 0,
        Basic = 1,
        Premium = 2
    }

    public enum PaymentStatus
    {
        Pending = 0,
        Paid = 1,
        Cancelled = 2,
        Expired = 3
    }

    public enum FileType
    {
        PDF = 0,
        DOCX = 1,
        PPTX = 2,
        TXT = 3,
        Other = 99
    }

    public enum MessageRole
    {
        User = 0,
        Assistant = 1,
        System = 2
    }

    public enum ExperimentStatus
    {
        Draft = 0,
        Running = 1,
        Completed = 2,
        Failed = 3
    }

    public enum ChunkingStrategy
    {
        Fixed = 0,
        Recursive = 1,
        Semantic = 2,
        Sentence = 3
    }

    public enum EmbeddingModel
    {
        MultilingualE5Base = 0,       // multilingual-e5-base (HuggingFace, cần token)
        TextEmbedding3Small = 1,      // text-embedding-3-small (OpenAI, cần API key)
        PhoBERTBase = 2,             // PhoBERT-base (HuggingFace, cần token)
        BgeM3 = 3,                   // bge-m3 (HuggingFace, cần token)
        GeminiEmbedding004 = 4       // text-embedding-004 (Gemini, miễn phí)
    }

    public enum RetrievalMethod
    {
        RAG = 0,
        FineTuned = 1,
        Hybrid = 2
    }
}
