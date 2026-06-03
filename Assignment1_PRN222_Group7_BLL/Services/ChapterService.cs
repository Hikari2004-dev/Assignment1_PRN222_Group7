using Assignment1_PRN222_Group7_DAL.Entities;
using Assignment1_PRN222_Group7_DAL.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7_BLL.Services
{
    public class ChapterService : IChapterService
    {
        private readonly IUnitOfWork _uow;

        public ChapterService(IUnitOfWork uow) => _uow = uow;

        public async Task<IEnumerable<Chapter>> GetChaptersBySubjectAsync(int subjectId)
        {
            var repo = _uow.GetRepository<Chapter>();
            var chapters = await repo.FindAsync(c => c.SubjectId == subjectId, "Subject", "Documents");
            return chapters.OrderBy(c => c.OrderIndex);
        }

        public async Task<Chapter?> GetChapterByIdAsync(int id)
        {
            var repo = _uow.GetRepository<Chapter>();
            var results = await repo.FindAsync(c => c.Id == id, "Subject");
            return results.FirstOrDefault();
        }

        public async Task CreateChapterAsync(Chapter chapter)
        {
            await _uow.GetRepository<Chapter>().AddAsync(chapter);
            await _uow.SaveChangesAsync();
        }

        public async Task UpdateChapterAsync(Chapter chapter)
        {
            _uow.GetRepository<Chapter>().Update(chapter);
            await _uow.SaveChangesAsync();
        }

        public async Task<bool> DeleteChapterAsync(int id)
        {
            var repo = _uow.GetRepository<Chapter>();
            var chapter = await repo.GetByIdAsync(id);
            if (chapter == null) return false;

            repo.Remove(chapter);
            await _uow.SaveChangesAsync();
            return true;
        }
    }
}
