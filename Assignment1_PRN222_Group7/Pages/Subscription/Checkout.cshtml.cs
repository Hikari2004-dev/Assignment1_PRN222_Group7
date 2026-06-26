using Assignment1_PRN222_Group7_BLL.Services;
using Assignment1_PRN222_Group7_DAL.Entities;
using Assignment1_PRN222_Group7_DAL.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7.Pages.Subscription
{
    [Authorize(Roles = "Student")]
    public class CheckoutModel : PageModel
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly IMoMoService _momoService;
        private readonly IVnPayService _vnPayService;

        public CheckoutModel(
            ISubscriptionService subscriptionService,
            IMoMoService momoService,
            IVnPayService vnPayService)
        {
            _subscriptionService = subscriptionService;
            _momoService = momoService;
            _vnPayService = vnPayService;
        }

        public SubscriptionPlan Plan { get; set; } = null!;
        public decimal FinalAmount { get; set; }
        public bool IsUpgrade { get; set; }

        private int GetUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        public async Task<IActionResult> OnGetAsync(int planId)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var userId = GetUserId();

            var plan = await _subscriptionService.GetPlanByIdAsync(planId);
            if (plan == null)
            {
                TempData["Error"] = "Gói dịch vụ không tồn tại.";
                return RedirectToPage("/Subscription/Index");
            }

            // Gói Free thì không cần thanh toán qua ngân hàng
            if (plan.Tier == SubscriptionTier.Free)
            {
                TempData["Error"] = "Bạn đã có gói Free mặc định.";
                return RedirectToPage("/Subscription/Index");
            }

            // Lấy thông tin gói đang hoạt động để so sánh/xác nhận
            var activeSubscription = await _subscriptionService.GetActiveSubscriptionAsync(userId);

            if (activeSubscription != null && activeSubscription.PlanId == plan.Id)
            {
                TempData["Error"] = "Bạn đang sử dụng gói này rồi.";
                return RedirectToPage("/Subscription/Index");
            }

            Plan = plan;
            FinalAmount = await _subscriptionService.GetUpgradeAmountAsync(userId, planId);
            IsUpgrade = activeSubscription != null && activeSubscription.Plan.Tier == SubscriptionTier.Basic && plan.Tier == SubscriptionTier.Premium;

            return Page();
        }

        public async Task<IActionResult> OnPostPayWithMomoAsync(int planId)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Yêu cầu nâng cấp gói không hợp lệ.";
                return RedirectToPage("/Subscription/Index");
            }
            var userId = GetUserId();

            var plan = await _subscriptionService.GetPlanByIdAsync(planId);
            if (plan == null || plan.Tier == SubscriptionTier.Free)
            {
                TempData["Error"] = "Yêu cầu nâng cấp gói không hợp lệ.";
                return RedirectToPage("/Subscription/Index");
            }

            var finalPrice = await _subscriptionService.GetUpgradeAmountAsync(userId, plan.Id);

            // Tạo URL thanh toán MoMo Sandbox qua BLL
            var payUrl = await _momoService.CreatePaymentUrlAsync(userId, plan.Id, plan.Name, finalPrice);
            if (string.IsNullOrEmpty(payUrl))
            {
                TempData["Error"] = "Không thể khởi tạo giao dịch với MoMo. Vui lòng thử lại sau.";
                return RedirectToPage("/Subscription/Index");
            }

            // Chuyển hướng người dùng sang trang thanh toán MoMo Sandbox
            return Redirect(payUrl);
        }

        public async Task<IActionResult> OnPostPayWithVnPayAsync(int planId)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Yêu cầu nâng cấp gói không hợp lệ.";
                return RedirectToPage("/Subscription/Index");
            }
            var userId = GetUserId();

            var plan = await _subscriptionService.GetPlanByIdAsync(planId);
            if (plan == null || plan.Tier == SubscriptionTier.Free)
            {
                TempData["Error"] = "Yêu cầu nâng cấp gói không hợp lệ.";
                return RedirectToPage("/Subscription/Index");
            }

            var finalPrice = await _subscriptionService.GetUpgradeAmountAsync(userId, plan.Id);

            // Lấy địa chỉ IP của Client
            string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            // Tạo URL thanh toán VNPAY Sandbox qua BLL
            var payUrl = _vnPayService.CreatePaymentUrl(ipAddress, userId, plan.Id, plan.Name, finalPrice);
            if (string.IsNullOrEmpty(payUrl))
            {
                TempData["Error"] = "Không thể khởi tạo giao dịch với VNPAY. Vui lòng thử lại sau.";
                return RedirectToPage("/Subscription/Index");
            }

            // Chuyển hướng người dùng sang trang thanh toán VNPAY Sandbox
            return Redirect(payUrl);
        }
    }
}
