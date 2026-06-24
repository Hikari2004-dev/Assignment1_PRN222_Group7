using Assignment1_PRN222_Group7_BLL.Services;
using Assignment1_PRN222_Group7_DAL.Entities;
using Assignment1_PRN222_Group7_DAL.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7.Pages.Document
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly IDocumentService _documentService;
        private readonly ISubjectService _subjectService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAccountService _accountService;

        public DetailsModel(
            IDocumentService documentService,
            ISubjectService subjectService,
            IUnitOfWork unitOfWork,
            IAccountService accountService)
        {
            _documentService = documentService;
            _subjectService = subjectService;
            _unitOfWork = unitOfWork;
            _accountService = accountService;
        }

        public Assignment1_PRN222_Group7_DAL.Entities.Subject Subject { get; set; } = null!;
        public Assignment1_PRN222_Group7_DAL.Entities.Document Document { get; set; } = null!;
        public IEnumerable<DocumentChunk> Chunks { get; set; } = null!;
        public bool IsAssignedLecturer { get; set; }

        private int GetUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private async Task<bool> HasSubjectAccessAsync(int subjectId)
        {
            if (User.IsInRole("Admin")) return true;
            if (User.IsInRole("Lecturer"))
            {
                var userId = GetUserId();
                return await _accountService.IsLecturerAssignedToSubjectAsync(userId, subjectId);
            }
            return true;
        }

        public async Task<IActionResult> OnGetAsync(int subjectId, int id)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (!await HasSubjectAccessAsync(subjectId)) return Forbid();

            var subject = await _subjectService.GetSubjectByIdAsync(subjectId);
            if (subject == null) return NotFound();

            var doc = await _documentService.GetDocumentByIdAsync(id);
            if (doc == null || doc.SubjectId != subjectId) return NotFound();

            Subject = subject;
            Document = doc;

            var chunkRepo = _unitOfWork.GetRepository<DocumentChunk>();
            var chunksList = await chunkRepo.FindAsync(c => c.DocumentId == id);
            Chunks = chunksList.OrderBy(c => c.ChunkIndex);

            IsAssignedLecturer = User.IsInRole("Lecturer") &&
                                 Subject.LecturerId.HasValue &&
                                 Subject.LecturerId.Value == GetUserId();

            return Page();
        }
    }
}
