using Assignment1_PRN222_Group7_DAL.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7_BLL.Services
{
    public interface ISubjectService
    {
        Task<IEnumerable<Subject>> GetAllSubjectsAsync();
        Task<Subject?> GetSubjectByIdAsync(int id);
        Task<bool> CreateSubjectAsync(Subject subject);
        Task<bool> UpdateSubjectAsync(Subject subject);
        Task<bool> DeleteSubjectAsync(int id);
        Task<bool> SubjectCodeExistsAsync(string code, int? excludeId = null);
    }
}
