using Assignment1_PRN222_Group7_BLL.Services;
using Assignment1_PRN222_Group7_DAL.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7.Controllers
{
    [Authorize]
    public class SubscriptionController : Controller
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly IMoMoService _momoService;

        public SubscriptionController(ISubscriptionService subscriptionService, IMoMoService momoService)
        {
            _subscriptionService = subscriptionService;
            _momoService = momoService;
        }

        // GET /Subscription
        public async Task<IActionResult> Index()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Lấy thông tin gói đang hoạt động của người dùng
            var activeSubscription = await _subscriptionService.GetActiveSubscriptionAsync(userId);

            // Lấy tất cả các gói dịch vụ có hiệu lực
            var plans = await _subscriptionService.GetActivePlansAsync();

            ViewBag.ActiveSubscription = activeSubscription;
            return View(plans);
        }

        // GET /Subscription/Checkout
        public async Task<IActionResult> Checkout(int planId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var plan = await _subscriptionService.GetPlanByIdAsync(planId);
            if (plan == null)
            {
                TempData["Error"] = "Gói dịch vụ không tồn tại.";
                return RedirectToAction("Index");
            }

            // Gói Free thì không cần thanh toán qua ngân hàng
            if (plan.Tier == SubscriptionTier.Free)
            {
                TempData["Error"] = "Bạn đã có gói Free mặc định.";
                return RedirectToAction("Index");
            }

            // Lấy thông tin gói đang hoạt động để so sánh/xác nhận
            var activeSubscription = await _subscriptionService.GetActiveSubscriptionAsync(userId);

            if (activeSubscription != null && activeSubscription.PlanId == plan.Id)
            {
                TempData["Error"] = "Bạn đang sử dụng gói này rồi.";
                return RedirectToAction("Index");
            }

            return View(plan);
        }

        // POST /Subscription/PayWithMomo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayWithMomo(int planId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var plan = await _subscriptionService.GetPlanByIdAsync(planId);
            if (plan == null || plan.Tier == SubscriptionTier.Free)
            {
                TempData["Error"] = "Yêu cầu nâng cấp gói không hợp lệ.";
                return RedirectToAction("Index");
            }

            // Tạo URL thanh toán MoMo Sandbox qua BLL
            var payUrl = await _momoService.CreatePaymentUrlAsync(userId, plan.Id, plan.Name, plan.Price);
            if (string.IsNullOrEmpty(payUrl))
            {
                TempData["Error"] = "Không thể khởi tạo giao dịch với MoMo. Vui lòng thử lại sau.";
                return RedirectToAction("Index");
            }

            // Chuyển hướng người dùng sang trang thanh toán MoMo Sandbox
            return Redirect(payUrl);
        }

        // GET /Subscription/MomoCallback
        [HttpGet]
        public async Task<IActionResult> MomoCallback(
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
            string signature)
        {
            // Lấy AccessKey từ HttpContext hoặc truyền cứng từ BLL.
            // Để khớp với tham số VerifySignature của BLL, ta truyền accessKey nhận lại từ truy vấn hoặc cấu hình.
            // MoMo gửi lại accessKey trong query string
            string accessKey = Request.Query["accessKey"].ToString();

            // 1. Xác thực chữ ký phản hồi từ MoMo
            var isValidSignature = _momoService.VerifySignature(
                accessKey, amount, extraData, message, orderId, orderInfo, 
                partnerCode, requestId, responseTime, resultCode, transId, signature
            );

            if (!isValidSignature)
            {
                TempData["Error"] = "Xác thực giao dịch thất bại. Chữ ký thanh toán không hợp lệ.";
                return RedirectToAction("Index");
            }

            // 2. Kiểm tra mã kết quả giao dịch (resultCode = 0 là thành công)
            if (resultCode != "0")
            {
                TempData["Error"] = $"Thanh toán thất bại: {message} (Mã lỗi: {resultCode})";
                return RedirectToAction("Index");
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
                return RedirectToAction("Index");
            }

            // 4. Kích hoạt nâng cấp gói dịch vụ qua BLL
            var success = await _subscriptionService.UpgradeSubscriptionAsync(userId, planId);
            if (!success)
            {
                TempData["Error"] = "Không thể cập nhật gói dịch vụ của bạn. Vui lòng liên hệ hỗ trợ.";
                return RedirectToAction("Index");
            }

            TempData["Success"] = "Thanh toán thành công! Tài khoản của bạn đã được nâng cấp.";
            return RedirectToAction("Index");
        }
    }
}
