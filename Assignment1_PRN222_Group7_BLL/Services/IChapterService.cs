using Assignment1_PRN222_Group7_DAL.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7_BLL.Services
{
    public interface IChapterService
    {
        Task<IEnumerable<Chapter>> GetChaptersBySubjectAsync(int subjectId);
        Task<Chapter?> GetChapterByIdAsync(int id);
        Task CreateChapterAsync(Chapter chapter);
        Task UpdateChapterAsync(Chapter chapter);
        Task<bool> DeleteChapterAsync(int id);
    }
}
