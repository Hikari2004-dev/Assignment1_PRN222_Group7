using System.Collections.Generic;
using Assignment1_PRN222_Group7_DAL.Enums;

namespace Assignment1_PRN222_Group7_BLL.Services
{
    public interface IChunkingService
    {
        List<string> ChunkText(string text, ChunkingStrategy strategy = ChunkingStrategy.Fixed, int chunkSize = 500, int overlap = 50);
    }
}
