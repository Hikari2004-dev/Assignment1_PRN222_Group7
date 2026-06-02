namespace Assignment1_PRN222_Group7_BLL.Services
{
    public interface IChunkingService
    {
        List<string> ChunkText(string text, int chunkSize = 500, int overlap = 50);
    }
}
