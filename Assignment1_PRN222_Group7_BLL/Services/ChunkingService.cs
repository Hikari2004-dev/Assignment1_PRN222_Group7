namespace Assignment1_PRN222_Group7_BLL.Services
{
    public class ChunkingService : IChunkingService
    {
        /// <summary>
        /// Fixed sliding window chunking by word count.
        /// </summary>
        public List<string> ChunkText(string text, int chunkSize = 500, int overlap = 50)
        {
            if (string.IsNullOrWhiteSpace(text)) return [];

            var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0) return [];

            var chunks = new List<string>();
            int step = Math.Max(1, chunkSize - overlap);

            for (int i = 0; i < words.Length; i += step)
            {
                int end = Math.Min(i + chunkSize, words.Length);
                chunks.Add(string.Join(' ', words[i..end]));
                if (end == words.Length) break;
            }

            return chunks;
        }
    }
}
