using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Assignment1_PRN222_Group7_BLL.Services;
using Assignment1_PRN222_Group7_DAL.Entities;
using Assignment1_PRN222_Group7_DAL.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Assignment1_PRN222_Group7.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly IChatService _chatService;
        private readonly ISubjectService _subjectService;
        private readonly ISubscriptionService _subscriptionService;

        public ChatController(IChatService chatService, ISubjectService subjectService, ISubscriptionService subscriptionService)
        {
            _chatService = chatService;
            _subjectService = subjectService;
            _subscriptionService = subscriptionService;
        }

        // GET: /Chat
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var subjects = await _subjectService.GetAllSubjectsAsync();
            ViewBag.Subjects = subjects;
            return View();
        }

        // GET: /Chat/GetSessions
        [HttpGet]
        public async Task<IActionResult> GetSessions()
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
            return Json(result);
        }

        // GET: /Chat/GetMessages
        [HttpGet]
        public async Task<IActionResult> GetMessages(int sessionId)
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

            return Json(result);
        }

        // POST: /Chat/CreateSession
        [HttpPost]
        public async Task<IActionResult> CreateSession(int? subjectId, string? title)
        {
            var userId = GetCurrentUserId();
            var session = await _chatService.CreateSessionAsync(userId, subjectId, title ?? "New Chat");
            return Json(new
            {
                session.Id,
                session.Title,
                CreatedAt = session.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }

        // POST: /Chat/SendMessage
        [HttpPost]
        public async Task<IActionResult> SendMessage(int sessionId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return BadRequest("Content cannot be empty.");
            }

            var userId = GetCurrentUserId();

            // Check subscription limit
            var (canChat, chatLimitMsg) = await _subscriptionService.CheckChatLimitAsync(userId);
            if (!canChat)
            {
                return Json(new { error = chatLimitMsg, limitExceeded = true });
            }

            var session = await _chatService.GetSessionWithMessagesAsync(sessionId);
            if (session == null || session.UserId != userId)
            {
                return Forbid();
            }

            try
            {
                var assistantMsg = await _chatService.SendMessageAsync(sessionId, content);

                // Fetch it again to load the sources and document info fully
                var updatedSession = await _chatService.GetSessionWithMessagesAsync(sessionId);
                var msgWithSources = updatedSession?.Messages.FirstOrDefault(m => m.Id == assistantMsg.Id);

                if (msgWithSources == null)
                {
                    return Json(new
                    {
                        assistantMsg.Id,
                        Role = assistantMsg.Role.ToString(),
                        assistantMsg.Content,
                        CreatedAt = assistantMsg.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                        Sources = new List<object>()
                    });
                }

                return Json(new
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

        // POST: /Chat/DeleteSession
        [HttpPost]
        public async Task<IActionResult> DeleteSession(int id)
        {
            var userId = GetCurrentUserId();
            var session = await _chatService.GetSessionWithMessagesAsync(id);
            if (session == null || session.UserId != userId)
            {
                return Forbid();
            }

            var success = await _chatService.DeleteSessionAsync(id);
            return Json(new { success });
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
