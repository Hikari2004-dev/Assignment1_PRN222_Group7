using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7_BLL.Services
{
    public interface IEmailService
    {
        Task SendAccountCredentialsAsync(string recipientEmail, string recipientName, string password, string roleName);
    }
}
