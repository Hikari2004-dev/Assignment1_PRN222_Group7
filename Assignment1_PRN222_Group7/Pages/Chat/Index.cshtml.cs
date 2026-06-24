using Assignment1_PRN222_Group7_BLL.Services;
using Assignment1_PRN222_Group7_DAL.Entities;
using Assignment1_PRN222_Group7_DAL.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7.Pages.Chat
{
    [Authorize]
    [IgnoreAntiforgeryToken]
    public class IndexModel : PageModel
    {
        private readonly IChatService _chatService;
        private readonly ISubjectService _subjectService;
        private readonly ISubscriptionService _subscriptionService;

        public IndexModel(IChatService chatService, ISubjectService subjectService, ISubscriptionService subscriptionService)
        {
            _chatService = chatService;
            _subjectService = subjectService;
            _subscriptionService = subscriptionService;
        }

        public List<Assignment1_PRN222_Group7_DAL.Entities.Subject> Subjects { get; set; } = [];

        public async Task<IActionResult> OnGetAsync()
        {
            var subjects = await _subjectService.GetAllSubjectsAsync();
            Subjects = subjects.ToList();
            return Page();
        }

        public async Task<IActionResult> OnGetGetSessionsAsync()
        {
            var userId = GetCurrentUserId();
            var sessions = await _chatService.GetUserSessionsAsync(userId);
            var result = sessions.Select(s => new
            {
                s.Id,
                s.Title,
                SubjectName = s.Subject?.Name ?? "General Chat",
                UpdatedAt = s.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                s.TotalMessages
            });
            return new JsonResult(result);
        }

        public async Task<IActionResult> OnGetGetMessagesAsync(int sessionId)
        {
            var userId = GetCurrentUserId();
            var session = await _chatService.GetSessionWithMessagesAsync(sessionId);
            if (session == null || session.UserId != userId)
            {
                return Forbid();
            }

            var result = session.Messages
                .OrderBy(m => m.CreatedAt)
                .Select(m => new
                {
                    m.Id,
                    Role = m.Role.ToString(),
                    m.Content,
                    CreatedAt = m.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    Sources = m.Sources.Select(s => new
                    {
                        s.Id,
                        DocumentTitle = s.Chunk?.Document?.Title ?? "Unknown Document",
                        ChunkIndex = s.Chunk?.ChunkIndex ?? 0,
                        s.CitedContent,
                        s.SimilarityScore
                    }).ToList()
                });

            return new JsonResult(result);
        }

        public async Task<IActionResult> OnPostCreateSessionAsync(int? subjectId, string? title)
        {
            var userId = GetCurrentUserId();
            var session = await _chatService.CreateSessionAsync(userId, subjectId, title ?? "New Chat");
            return new JsonResult(new
            {
                session.Id,
                session.Title,
                CreatedAt = session.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }

        public async Task<IActionResult> OnPostSendMessageAsync(int sessionId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return BadRequest("Content cannot be empty.");
            }

            var userId = GetCurrentUserId();

            var (canChat, chatLimitMsg) = await _subscriptionService.CheckChatLimitAsync(userId);
            if (!canChat)
            {
                return new JsonResult(new { error = chatLimitMsg, limitExceeded = true });
            }

            var session = await _chatService.GetSessionWithMessagesAsync(sessionId);
            if (session == null || session.UserId != userId)
            {
                return Forbid();
            }

            try
            {
                var assistantMsg = await _chatService.SendMessageAsync(sessionId, content);

                var updatedSession = await _chatService.GetSessionWithMessagesAsync(sessionId);
                var msgWithSources = updatedSession?.Messages.FirstOrDefault(m => m.Id == assistantMsg.Id);

                if (msgWithSources == null)
                {
                    return new JsonResult(new
                    {
                        assistantMsg.Id,
                        Role = assistantMsg.Role.ToString(),
                        assistantMsg.Content,
                        CreatedAt = assistantMsg.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                        Sources = new List<object>()
                    });
                }

                return new JsonResult(new
                {
                    msgWithSources.Id,
                    Role = msgWithSources.Role.ToString(),
                    msgWithSources.Content,
                    CreatedAt = msgWithSources.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                    Sources = msgWithSources.Sources.Select(s => new
                    {
                        s.Id,
                        DocumentTitle = s.Chunk?.Document?.Title ?? "Unknown Document",
                        ChunkIndex = s.Chunk?.ChunkIndex ?? 0,
                        s.CitedContent,
                        s.SimilarityScore
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        public async Task<IActionResult> OnPostDeleteSessionAsync(int id)
        {
            var userId = GetCurrentUserId();
            var session = await _chatService.GetSessionWithMessagesAsync(id);
            if (session == null || session.UserId != userId)
            {
                return Forbid();
            }

            var success = await _chatService.DeleteSessionAsync(id);
            return new JsonResult(new { success });
        }

        private int GetCurrentUserId()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                throw new InvalidOperationException("User ID is missing or invalid in claims.");
            }
            return userId;
        }
    }
}
