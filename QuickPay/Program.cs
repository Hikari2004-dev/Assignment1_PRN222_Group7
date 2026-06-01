using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;

namespace QuickPay
{
    class Program
    {
        private static readonly HttpClient client = new HttpClient();

        static async Task Main(string[] args)
        {
            Guid myuuid = Guid.NewGuid();
            string myuuidAsString = myuuid.ToString();

            string accessKey = "F8BBA842ECF85";
            string secretKey = "K951B6PE1waDMi640xX08PD3vg6EkVlz";

            string partnerCode = "MOMO";
            string requestId = myuuidAsString;
            long amount = 5000;
            string orderId = myuuidAsString;
            string orderInfo = "pay with MoMo";
            string redirectUrl = "http://localhost:5054/Subscription/MomoCallback";
            string ipnUrl = "https://webhook.site/b3088a6a-2d17-4f8d-a383-71389a6c600b";
            string extraData = "";
            string requestType = "captureWallet";

            // Chuỗi signature chuẩn cho captureWallet MoMo v2:
            // accessKey=$accessKey&amount=$amount&extraData=$extraData&ipnUrl=$ipnUrl&orderId=$orderId&orderInfo=$orderInfo&partnerCode=$partnerCode&redirectUrl=$redirectUrl&requestId=$requestId&requestType=$requestType
            var rawSignature = $"accessKey={accessKey}&amount={amount}&extraData={extraData}&ipnUrl={ipnUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={partnerCode}&redirectUrl={redirectUrl}&requestId={requestId}&requestType={requestType}";
            string signature = getSignature(rawSignature, secretKey);

            var payload = new
            {
                partnerCode = partnerCode,
                partnerName = "MoMo Payment",
                storeId = "Test Store",
                requestId = requestId,
                amount = amount,
                orderId = orderId,
                orderInfo = orderInfo,
                redirectUrl = redirectUrl,
                ipnUrl = ipnUrl,
                lang = "vi",
                extraData = extraData,
                requestType = requestType,
                signature = signature
            };

            StringContent httpContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var quickPayResponse = await client.PostAsync("https://test-payment.momo.vn/v2/gateway/api/create", httpContent);
            var contents = await quickPayResponse.Content.ReadAsStringAsync();
            System.Console.WriteLine(contents + "");
        }

        private static String getSignature(String text, String key)
        {
            UTF8Encoding encoding = new UTF8Encoding();

            Byte[] textBytes = encoding.GetBytes(text);
            Byte[] keyBytes = encoding.GetBytes(key);

            Byte[] hashBytes;

            using (HMACSHA256 hash = new HMACSHA256(keyBytes))
                hashBytes = hash.ComputeHash(textBytes);

            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }
}
