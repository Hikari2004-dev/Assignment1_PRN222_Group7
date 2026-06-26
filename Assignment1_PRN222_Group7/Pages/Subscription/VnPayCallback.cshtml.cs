using Assignment1_PRN222_Group7_BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Linq;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7.Pages.Subscription
{
    [Authorize(Roles = "Student")]
    public class VnPayCallbackModel : PageModel
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly IVnPayService _vnPayService;

        public VnPayCallbackModel(ISubscriptionService subscriptionService, IVnPayService vnPayService)
        {
            _subscriptionService = subscriptionService;
            _vnPayService = vnPayService;
        }

        public async Task<IActionResult> OnGetAsync(
            [FromQuery] string vnp_ResponseCode,
            [FromQuery] string vnp_TransactionNo,
            [FromQuery] string vnp_TxnRef)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var queryParams = Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString());

            // 1. Xác thực chữ ký phản hồi từ VNPAY
            var isValidSignature = _vnPayService.ValidateSignature(queryParams);
            if (!isValidSignature)
            {
                TempData["Error"] = "Xác thực giao dịch thất bại. Chữ ký thanh toán không hợp lệ.";
                return RedirectToPage("/Subscription/Index");
            }

            // 2. Lấy các tham số phản hồi
            string responseCode = vnp_ResponseCode;
            string transactionNo = vnp_TransactionNo;
            string txnRef = vnp_TxnRef;

            // txnRef định dạng: userId_planId_guid
            int userId = 0;
            int planId = 0;
            if (!string.IsNullOrEmpty(txnRef))
            {
                var parts = txnRef.Split('_');
                if (parts.Length >= 2)
                {
                    int.TryParse(parts[0], out userId);
                    int.TryParse(parts[1], out planId);
                }
            }

            if (userId == 0 || planId == 0)
            {
                TempData["Error"] = "Thông tin giao dịch không hợp lệ.";
                return RedirectToPage("/Subscription/Index");
            }

            // 3. Kiểm tra mã phản hồi giao dịch (00 là thành công)
            if (responseCode != "00")
            {
                TempData["Error"] = $"Thanh toán VNPAY thất bại. Mã phản hồi: {responseCode}";
                return RedirectToPage("/Subscription/Index");
            }

            // 4. Kích hoạt nâng cấp gói dịch vụ qua BLL
            var success = await _subscriptionService.UpgradeSubscriptionAsync(userId, planId, transactionNo);
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
