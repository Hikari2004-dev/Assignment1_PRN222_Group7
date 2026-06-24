using Assignment1_PRN222_Group7_BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7.Pages.Subscription
{
    [Authorize]
    public class MomoCallbackModel : PageModel
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly IMoMoService _momoService;

        public MomoCallbackModel(ISubscriptionService subscriptionService, IMoMoService momoService)
        {
            _subscriptionService = subscriptionService;
            _momoService = momoService;
        }

        public async Task<IActionResult> OnGetAsync(
            string partnerCode,
            string orderId,
            string requestId,
            string amount,
            string orderInfo,
            string orderType,
            string transId,
            string resultCode,
            string message,
            string payType,
            string responseTime,
            string extraData,
            string signature,
            [FromQuery] string accessKey)
        {
            // 1. Xác thực chữ ký phản hồi từ MoMo
            var isValidSignature = _momoService.VerifySignature(
                accessKey, amount, extraData, message, orderId, orderInfo, 
                partnerCode, requestId, responseTime, resultCode, transId, signature
            );

            if (!isValidSignature)
            {
                TempData["Error"] = "Xác thực giao dịch thất bại. Chữ ký thanh toán không hợp lệ.";
                return RedirectToPage("/Subscription/Index");
            }

            // 2. Kiểm tra mã kết quả giao dịch (resultCode = 0 là thành công)
            if (resultCode != "0")
            {
                TempData["Error"] = $"Thanh toán thất bại: {message} (Mã lỗi: {resultCode})";
                return RedirectToPage("/Subscription/Index");
            }

            // 3. Giải mã extraData để lấy userId và planId
            int userId = 0;
            int planId = 0;
            if (!string.IsNullOrEmpty(extraData))
            {
                var parts = extraData.Split(';');
                foreach (var part in parts)
                {
                    var kv = part.Split('=');
                    if (kv.Length == 2)
                    {
                        if (kv[0] == "userId") int.TryParse(kv[1], out userId);
                        if (kv[0] == "planId") int.TryParse(kv[1], out planId);
                    }
                }
            }

            if (userId == 0 || planId == 0)
            {
                TempData["Error"] = "Không thể xử lý thông tin tài khoản từ dữ liệu thanh toán.";
                return RedirectToPage("/Subscription/Index");
            }

            // 4. Kích hoạt nâng cấp gói dịch vụ qua BLL
            var success = await _subscriptionService.UpgradeSubscriptionAsync(userId, planId, transId);
            if (!success)
            {
                TempData["Error"] = "Không thể cập nhật gói dịch vụ của bạn. Vui lòng liên hệ hỗ trợ.";
                return RedirectToPage("/Subscription/Index");
            }

            TempData["Success"] = "Thanh toán thành công! Tài khoản của bạn đã được nâng cấp.";
            return RedirectToPage("/Subscription/Index");
        }
    }
}
