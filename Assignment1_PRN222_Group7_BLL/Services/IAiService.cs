using System.Collections.Generic;
using System.Threading.Tasks;
using Assignment1_PRN222_Group7_DAL.Entities;

namespace Assignment1_PRN222_Group7_BLL.Services
{
    public interface IAiService
    {
        /// <summary>
        /// Generates content using the configured LLM, incorporating the conversation history.
        /// </summary>
        /// <param name="prompt">The current query prompt (usually augmented with context)</param>
        /// <param name="history">Optional chat history for multi-turn conversations</param>
        /// <returns>Generated text response</returns>
        Task<string> GenerateContentAsync(string prompt, List<ChatMessage>? history = null);
    }
}
