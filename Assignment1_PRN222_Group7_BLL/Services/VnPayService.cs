using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Assignment1_PRN222_Group7_BLL.Services
{
    public class VnPayService : IVnPayService
    {
        private readonly IConfiguration _configuration;

        public VnPayService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string CreatePaymentUrl(string ipAddress, int userId, int planId, string planName, decimal amount)
        {
            var config = _configuration.GetSection("VnPayConfig");
            string tmnCode = config["TmnCode"] ?? "";
            string hashSecret = config["HashSecret"] ?? "";
            string baseUrl = config["BaseUrl"] ?? "";
            string returnUrl = config["ReturnUrl"] ?? "";

            var vnPay = new VnPayLibrary();
            vnPay.AddRequestData("vnp_Version", "2.1.0");
            vnPay.AddRequestData("vnp_Command", "pay");
            vnPay.AddRequestData("vnp_TmnCode", tmnCode);
            vnPay.AddRequestData("vnp_Amount", ((long)(amount * 100)).ToString()); // VNPAY amount is in VND * 100
            
            // Format unique transaction ref as: userId_planId_guid
            string txnRef = $"{userId}_{planId}_{Guid.NewGuid().ToString("N").Substring(0, 12)}";
            vnPay.AddRequestData("vnp_TxnRef", txnRef);
            
            vnPay.AddRequestData("vnp_OrderInfo", $"Thanh toan goi {planName} cho Hikari Chatbot");
            vnPay.AddRequestData("vnp_OrderType", "other");
            vnPay.AddRequestData("vnp_Locale", "vn");
            vnPay.AddRequestData("vnp_ReturnUrl", returnUrl);
            vnPay.AddRequestData("vnp_IpAddr", string.IsNullOrEmpty(ipAddress) ? "127.0.0.1" : ipAddress);
            
            // Vietnam Timezone is GMT+7 (fixed offset, platform independent)
            var localTime = DateTime.UtcNow.AddHours(7);
            vnPay.AddRequestData("vnp_CreateDate", localTime.ToString("yyyyMMddHHmmss"));
            vnPay.AddRequestData("vnp_CurrCode", "VND");

            return vnPay.CreateRequestUrl(baseUrl, hashSecret);
        }

        public bool ValidateSignature(Dictionary<string, string> queryParameters)
        {
            var config = _configuration.GetSection("VnPayConfig");
            string hashSecret = config["HashSecret"] ?? "";

            var vnPay = new VnPayLibrary();
            foreach (var kv in queryParameters)
            {
                if (kv.Key.StartsWith("vnp_"))
                {
                    vnPay.AddResponseData(kv.Key, kv.Value);
                }
            }

            string? incomingSecureHash = queryParameters.GetValueOrDefault("vnp_SecureHash");
            if (string.IsNullOrEmpty(incomingSecureHash))
            {
                return false;
            }

            return vnPay.ValidateSignature(incomingSecureHash, hashSecret);
        }
    }

    public class VnPayLibrary
    {
        private readonly SortedList<string, string> _requestData = new(new VnPayCompare());
        private readonly SortedList<string, string> _responseData = new(new VnPayCompare());

        public void AddRequestData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _requestData.Add(key, value);
            }
        }

        public void AddResponseData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _responseData.Add(key, value);
            }
        }

        public string CreateRequestUrl(string baseUrl, string hashSecret)
        {
            var data = new StringBuilder();
            foreach (var kv in _requestData)
            {
                data.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
            }
            string rawData = data.ToString().TrimEnd('&');
            string secureHash = HmacSHA512(hashSecret, rawData);
            
            string requestUrl = baseUrl + "?" + rawData + "&vnp_SecureHash=" + secureHash;
            return requestUrl;
        }

        public bool ValidateSignature(string inputHash, string secretKey)
        {
            _responseData.Remove("vnp_SecureHash");
            _responseData.Remove("vnp_SecureHashType");

            var data = new StringBuilder();
            foreach (var kv in _responseData)
            {
                data.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
            }
            string rawData = data.ToString().TrimEnd('&');
            string myChecksum = HmacSHA512(secretKey, rawData);
            return myChecksum.Equals(inputHash, StringComparison.OrdinalIgnoreCase);
        }

        private static string HmacSHA512(string key, string inputData)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
            using var hmac = new HMACSHA512(keyBytes);
            byte[] hashValue = hmac.ComputeHash(inputBytes);
            
            var hash = new StringBuilder(128);
            foreach (byte theByte in hashValue)
            {
                hash.Append(theByte.ToString("x2"));
            }
            return hash.ToString().ToUpper();
        }
    }

    public class VnPayCompare : IComparer<string>
    {
        public int Compare(string? x, string? y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            return string.CompareOrdinal(x, y);
        }
    }
}
