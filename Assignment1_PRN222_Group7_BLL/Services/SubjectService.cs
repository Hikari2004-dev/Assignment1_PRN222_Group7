using Assignment1_PRN222_Group7_DAL.Entities;
using Assignment1_PRN222_Group7_DAL.Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7_BLL.Services
{
    public class SubjectService : ISubjectService
    {
        private readonly IUnitOfWork _uow;

        public SubjectService(IUnitOfWork uow) => _uow = uow;

        public async Task<IEnumerable<Subject>> GetAllSubjectsAsync()
        {
            var repo = _uow.GetRepository<Subject>();
            return await repo.FindAsync(s => true, "Creator", "Chapters", "Lecturer");
        }

        public async Task<Subject?> GetSubjectByIdAsync(int id)
        {
            var repo = _uow.GetRepository<Subject>();
            var results = await repo.FindAsync(s => s.Id == id, "Creator", "Chapters", "Lecturer");
            return results.FirstOrDefault();
        }

        public async Task<bool> CreateSubjectAsync(Subject subject)
        {
            if (await SubjectCodeExistsAsync(subject.Code))
                return false;

            await _uow.GetRepository<Subject>().AddAsync(subject);
            await _uow.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateSubjectAsync(Subject subject)
        {
            if (await SubjectCodeExistsAsync(subject.Code, subject.Id))
                return false;

            _uow.GetRepository<Subject>().Update(subject);
            await _uow.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteSubjectAsync(int id)
        {
            var repo = _uow.GetRepository<Subject>();
            var subject = await repo.GetByIdAsync(id);
            if (subject == null) return false;

            repo.Remove(subject);
            await _uow.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SubjectCodeExistsAsync(string code, int? excludeId = null)
        {
            var repo = _uow.GetRepository<Subject>();
            if (excludeId.HasValue)
                return await repo.AnyAsync(s => s.Code == code && s.Id != excludeId.Value);
            return await repo.AnyAsync(s => s.Code == code);
        }
    }
}
