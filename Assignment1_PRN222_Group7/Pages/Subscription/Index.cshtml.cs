using Assignment1_PRN222_Group7_BLL.Services;
using Assignment1_PRN222_Group7_DAL.Entities;
using Assignment1_PRN222_Group7_DAL.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7.Pages.Subscription
{
    [Authorize(Roles = "Student")]
    public class IndexModel : PageModel
    {
        private readonly ISubscriptionService _subscriptionService;

        public IndexModel(ISubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService;
        }

        public UserSubscription? ActiveSubscription { get; set; }
        public UserSubscription? PendingSubscription { get; set; }
        public List<SubscriptionPlan> Plans { get; set; } = null!;

        private int GetUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = GetUserId();

            ActiveSubscription = await _subscriptionService.GetActiveSubscriptionAsync(userId);
            PendingSubscription = await _subscriptionService.GetPendingSubscriptionAsync(userId);
            Plans = await _subscriptionService.GetActivePlansAsync();

            return Page();
        }

        public IActionResult OnPost()
        {
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostCancelAutoRenewAsync()
        {
            var userId = GetUserId();
            var success = await _subscriptionService.CancelAutoRenewAsync(userId);
            if (success)
            {
                TempData["Success"] = "Đã hủy gia hạn tự động thành công. Tài khoản sẽ tự động chuyển về gói Free sau khi hết hạn.";
            }
            else
            {
                TempData["Error"] = "Không thể hủy gia hạn tự động hoặc bạn không có gói dịch vụ nào đang hoạt động.";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostScheduleDowngradeAsync(int planId)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Dữ liệu không hợp lệ.";
                return RedirectToPage();
            }

            var userId = GetUserId();
            var success = await _subscriptionService.ScheduleDowngradeAsync(userId, planId);
            if (success)
            {
                TempData["Success"] = "Đã đặt lịch hạ gói thành công. Gói dịch vụ của bạn sẽ tự động chuyển đổi khi gói Premium hiện tại hết hạn.";
            }
            else
            {
                TempData["Error"] = "Độc quyền cho gói Premium hoặc gói đặt lịch không hợp lệ.";
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostCancelScheduledDowngradeAsync()
        {
            var userId = GetUserId();
            var success = await _subscriptionService.CancelScheduledDowngradeAsync(userId);
            if (success)
            {
                TempData["Success"] = "Đã hủy lịch hạ gói thành công.";
            }
            else
            {
                TempData["Error"] = "Không thể hủy lịch hạ gói.";
            }
            return RedirectToPage();
        }
    }
}
