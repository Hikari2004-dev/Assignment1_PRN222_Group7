using Assignment1_PRN222_Group7_DAL.Entities;
using Assignment1_PRN222_Group7_BLL.Models;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7_BLL.Services
{
    public interface IAccountService
    {
        Task<User?> LoginAsync(string email, string password);
        Task<bool> RegisterAsync(string fullName, string email, string? password, int roleId, string? studentOrStaffId = null);
        Task<BulkRegisterResult> RegisterBulkAsync(Stream excelStream);
        Task UpdateLastLoginAsync(int userId);
        Task<List<Role>> GetAvailableRolesAsync();

        // User Management for Admin
        Task<List<User>> GetAllUsersAsync();
        Task<User?> GetUserByIdAsync(int id);
        Task<bool> UpdateUserAsync(User user);
        Task<bool> ToggleUserStatusAsync(int userId);

        // Lecturer-Subject Assignment for Admin
        Task<List<Subject>> GetSubjectsAssignedToLecturerAsync(int lecturerId);
        Task<bool> AssignSubjectsToLecturerAsync(int lecturerId, List<int> subjectIds);
        Task<bool> IsLecturerAssignedToSubjectAsync(int lecturerId, int subjectId);
    }
}
