using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7_BLL.Services
{
    public interface IMoMoService
    {
        Task<string?> CreatePaymentUrlAsync(int userId, int planId, string planName, decimal amount);
        bool VerifySignature(string accessKey, string amount, string extraData, string message, string orderId, string orderInfo, string partnerCode, string requestId, string responseTime, string resultCode, string transId, string responseSignature);
    }
}
