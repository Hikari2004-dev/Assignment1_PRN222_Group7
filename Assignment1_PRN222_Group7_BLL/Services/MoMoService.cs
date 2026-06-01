using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;

namespace Assignment1_PRN222_Group7_BLL.Services
{
    public class MoMoService : IMoMoService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public MoMoService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<string?> CreatePaymentUrlAsync(int userId, int planId, string planName, decimal amount)
        {
            var momoSection = _configuration.GetSection("MomoConfig");
            string partnerCode = momoSection["PartnerCode"] ?? "MOMO";
            string accessKey = momoSection["AccessKey"] ?? "";
            string secretKey = momoSection["SecretKey"] ?? "";
            string createUrl = momoSection["CreateUrl"] ?? "";
            string redirectUrl = momoSection["RedirectUrl"] ?? "";
            string ipnUrl = momoSection["IpnUrl"] ?? "";

            string requestId = Guid.NewGuid().ToString();
            string orderId = Guid.NewGuid().ToString();
            
            // Đóng gói userId và planId vào extraData để lúc callback nhận lại và nâng cấp
            string extraData = $"userId={userId};planId={planId}";
            string orderInfo = $"Thanh toan goi {planName} cho Hikari Chatbot";
            long amountLong = Convert.ToInt64(amount);

            // Xây dựng chuỗi ký chuẩn cho captureWallet MoMo v2:
            // accessKey=$accessKey&amount=$amount&extraData=$extraData&ipnUrl=$ipnUrl&orderId=$orderId&orderInfo=$orderInfo&partnerCode=$partnerCode&redirectUrl=$redirectUrl&requestId=$requestId&requestType=$requestType
            string rawSignature = $"accessKey={accessKey}&amount={amountLong}&extraData={extraData}&ipnUrl={ipnUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={partnerCode}&redirectUrl={redirectUrl}&requestId={requestId}&requestType=captureWallet";
            string signature = GetSignature(rawSignature, secretKey);

            var requestPayload = new
            {
                partnerCode = partnerCode,
                partnerName = "Hikari Chatbot MoMo Payment",
                storeId = "Hikari Store",
                requestId = requestId,
                amount = amountLong,
                orderId = orderId,
                orderInfo = orderInfo,
                redirectUrl = redirectUrl,
                ipnUrl = ipnUrl,
                lang = "vi",
                extraData = extraData,
                requestType = "captureWallet",
                signature = signature
            };

            var jsonPayload = JsonSerializer.Serialize(requestPayload);
            var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(createUrl, httpContent);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(jsonResponse);
                var root = doc.RootElement;

                if (root.TryGetProperty("resultCode", out var resultCodeElement) && resultCodeElement.GetInt32() == 0)
                {
                    if (root.TryGetProperty("payUrl", out var payUrlElement))
                    {
                        return payUrlElement.GetString();
                    }
                }
                
                return null;
            }
            catch
            {
                return null;
            }
        }

        public bool VerifySignature(string accessKey, string amount, string extraData, string message, string orderId, string orderInfo, string partnerCode, string requestId, string responseTime, string resultCode, string transId, string responseSignature)
        {
            var momoSection = _configuration.GetSection("MomoConfig");
            string secretKey = momoSection["SecretKey"] ?? "";

            // Định dạng raw signature phản hồi redirect của MoMo:
            // accessKey=$accessKey&amount=$amount&extraData=$extraData&message=$message&orderId=$orderId&orderInfo=$orderInfo&partnerCode=$partnerCode&requestId=$requestId&responseTime=$responseTime&resultCode=$resultCode&transId=$transId
            string rawSignature = $"accessKey={accessKey}&amount={amount}&extraData={extraData}&message={message}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={partnerCode}&requestId={requestId}&responseTime={responseTime}&resultCode={resultCode}&transId={transId}";
            string calculatedSignature = GetSignature(rawSignature, secretKey);

            return calculatedSignature.Equals(responseSignature, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetSignature(string text, string key)
        {
            var encoding = new UTF8Encoding();
            byte[] textBytes = encoding.GetBytes(text);
            byte[] keyBytes = encoding.GetBytes(key);

            byte[] hashBytes;
            using (var hash = new HMACSHA256(keyBytes))
            {
                hashBytes = hash.ComputeHash(textBytes);
            }

            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }
}
