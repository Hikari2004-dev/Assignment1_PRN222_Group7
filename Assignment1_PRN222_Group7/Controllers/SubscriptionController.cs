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
        private readonly IVnPayService _vnPayService;

        public SubscriptionController(
            ISubscriptionService subscriptionService, 
            IMoMoService momoService,
            IVnPayService vnPayService)
        {
            _subscriptionService = subscriptionService;
            _momoService = momoService;
            _vnPayService = vnPayService;
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

            // Lấy gói pending chờ kích hoạt nếu có
            var pendingSubscription = await _subscriptionService.GetPendingSubscriptionAsync(userId);

            // Lấy tất cả các gói dịch vụ có hiệu lực
            var plans = await _subscriptionService.GetActivePlansAsync();

            ViewBag.ActiveSubscription = activeSubscription;
            ViewBag.PendingSubscription = pendingSubscription;
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

            var finalAmount = await _subscriptionService.GetUpgradeAmountAsync(userId, planId);
            ViewBag.FinalAmount = finalAmount;
            ViewBag.IsUpgrade = activeSubscription != null && activeSubscription.Plan.Tier == SubscriptionTier.Basic && plan.Tier == SubscriptionTier.Premium;

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

            var finalPrice = await _subscriptionService.GetUpgradeAmountAsync(userId, plan.Id);

            // Tạo URL thanh toán MoMo Sandbox qua BLL
            var payUrl = await _momoService.CreatePaymentUrlAsync(userId, plan.Id, plan.Name, finalPrice);
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
            var success = await _subscriptionService.UpgradeSubscriptionAsync(userId, planId, transId);
            if (!success)
            {
                TempData["Error"] = "Không thể cập nhật gói dịch vụ của bạn. Vui lòng liên hệ hỗ trợ.";
                return RedirectToAction("Index");
            }

            TempData["Success"] = "Thanh toán thành công! Tài khoản của bạn đã được nâng cấp.";
            return RedirectToAction("Index");
        }

        // POST /Subscription/PayWithVnPay
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayWithVnPay(int planId)
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

            var finalPrice = await _subscriptionService.GetUpgradeAmountAsync(userId, plan.Id);

            // Lấy địa chỉ IP của Client
            string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            // Tạo URL thanh toán VNPAY Sandbox qua BLL
            var payUrl = _vnPayService.CreatePaymentUrl(ipAddress, userId, plan.Id, plan.Name, finalPrice);
            if (string.IsNullOrEmpty(payUrl))
            {
                TempData["Error"] = "Không thể khởi tạo giao dịch với VNPAY. Vui lòng thử lại sau.";
                return RedirectToAction("Index");
            }

            // Chuyển hướng người dùng sang trang thanh toán VNPAY Sandbox
            return Redirect(payUrl);
        }

        // GET /Subscription/VnPayCallback
        [HttpGet]
        public async Task<IActionResult> VnPayCallback()
        {
            var queryParams = Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString());

            // 1. Xác thực chữ ký phản hồi từ VNPAY
            var isValidSignature = _vnPayService.ValidateSignature(queryParams);
            if (!isValidSignature)
            {
                TempData["Error"] = "Xác thực giao dịch thất bại. Chữ ký thanh toán không hợp lệ.";
                return RedirectToAction("Index");
            }

            // 2. Lấy các tham số phản hồi
            string responseCode = Request.Query["vnp_ResponseCode"].ToString();
            string transactionNo = Request.Query["vnp_TransactionNo"].ToString();
            string txnRef = Request.Query["vnp_TxnRef"].ToString();

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
                return RedirectToAction("Index");
            }

            // 3. Kiểm tra mã phản hồi giao dịch (00 là thành công)
            if (responseCode != "00")
            {
                TempData["Error"] = $"Thanh toán VNPAY thất bại. Mã phản hồi: {responseCode}";
                return RedirectToAction("Index");
            }

            // 4. Kích hoạt nâng cấp gói dịch vụ qua BLL
            var success = await _subscriptionService.UpgradeSubscriptionAsync(userId, planId, transactionNo);
            if (!success)
            {
                TempData["Error"] = "Không thể cập nhật gói dịch vụ của bạn. Vui lòng liên hệ hỗ trợ.";
                return RedirectToAction("Index");
            }

            TempData["Success"] = "Thanh toán thành công! Tài khoản của bạn đã được nâng cấp.";
            return RedirectToAction("Index");
        }

        // GET /Subscription/VnPayIpn
        [HttpGet]
        public async Task<IActionResult> VnPayIpn()
        {
            var queryParams = Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString());

            // 1. Xác thực chữ ký phản hồi từ VNPAY
            var isValidSignature = _vnPayService.ValidateSignature(queryParams);
            if (!isValidSignature)
            {
                return Json(new { RspCode = "97", Message = "Invalid signature" });
            }

            // 2. Lấy tham số phản hồi
            string responseCode = Request.Query["vnp_ResponseCode"].ToString();
            string transactionNo = Request.Query["vnp_TransactionNo"].ToString();
            string txnRef = Request.Query["vnp_TxnRef"].ToString();

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
                return Json(new { RspCode = "01", Message = "Order not found" });
            }

            // 3. Kiểm tra trạng thái giao dịch
            if (responseCode == "00")
            {
                // Kiểm tra xem gói đã được nâng cấp cho giao dịch này chưa (để tránh trùng lặp nếu callback đã làm trước đó)
                var activeSub = await _subscriptionService.GetActiveSubscriptionAsync(userId);
                if (activeSub != null && activeSub.PlanId == planId && activeSub.TransactionId == transactionNo)
                {
                    return Json(new { RspCode = "02", Message = "Order already confirmed" });
                }

                var success = await _subscriptionService.UpgradeSubscriptionAsync(userId, planId, transactionNo);
                if (!success)
                {
                    return Json(new { RspCode = "99", Message = "Input required data invalid / upgrade failed" });
                }
            }

            return Json(new { RspCode = "00", Message = "Confirm success" });
        }

        // POST /Subscription/CancelAutoRenew
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAutoRenew()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var success = await _subscriptionService.CancelAutoRenewAsync(userId);
            if (success)
            {
                TempData["Success"] = "Đã hủy gia hạn tự động thành công. Tài khoản sẽ tự động chuyển về gói Free sau khi hết hạn.";
            }
            else
            {
                TempData["Error"] = "Không thể hủy gia hạn tự động hoặc bạn không có gói dịch vụ nào đang hoạt động.";
            }
            return RedirectToAction("Index");
        }

        // POST /Subscription/ScheduleDowngrade
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ScheduleDowngrade(int planId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var success = await _subscriptionService.ScheduleDowngradeAsync(userId, planId);
            if (success)
            {
                TempData["Success"] = "Đã đặt lịch hạ gói thành công. Gói dịch vụ của bạn sẽ tự động chuyển đổi khi gói Premium hiện tại hết hạn.";
            }
            else
            {
                TempData["Error"] = "Độc quyền cho gói Premium hoặc gói đặt lịch không hợp lệ.";
            }
            return RedirectToAction("Index");
        }

        // POST /Subscription/CancelScheduledDowngrade
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelScheduledDowngrade()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var success = await _subscriptionService.CancelScheduledDowngradeAsync(userId);
            if (success)
            {
                TempData["Success"] = "Đã hủy lịch hạ gói thành công.";
            }
            else
            {
                TempData["Error"] = "Không thể hủy lịch hạ gói.";
            }
            return RedirectToAction("Index");
        }
    }
}
