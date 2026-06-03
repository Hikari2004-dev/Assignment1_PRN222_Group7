using System.Collections.Generic;

namespace Assignment1_PRN222_Group7_BLL.Services
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(string ipAddress, int userId, int planId, string planName, decimal amount);
        bool ValidateSignature(Dictionary<string, string> queryParameters);
    }
}
