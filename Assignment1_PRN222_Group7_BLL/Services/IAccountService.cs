using Assignment1_PRN222_Group7_DAL.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7_BLL.Services
{
    public interface IAccountService
    {
        Task<User?> LoginAsync(string email, string password);
        Task<bool> RegisterAsync(string fullName, string email, string password, int roleId);
        Task UpdateLastLoginAsync(int userId);
        Task<List<Role>> GetAvailableRolesAsync();
    }
}
