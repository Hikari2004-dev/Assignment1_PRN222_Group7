using System.Collections.Generic;
using System.Threading.Tasks;
using Assignment1_PRN222_Group7_DAL.Entities;

namespace Assignment1_PRN222_Group7_BLL.Services
{
    public interface IChatService
    {
        Task<ChatSession> CreateSessionAsync(int userId, int? subjectId, string title = "New Chat");
        Task<ChatSession?> GetSessionWithMessagesAsync(int sessionId);
        Task<IEnumerable<ChatSession>> GetUserSessionsAsync(int userId);
        Task<bool> DeleteSessionAsync(int sessionId);
        Task<ChatMessage> SendMessageAsync(int sessionId, string userContent);
    }
}
