using Assignment1_PRN222_Group7_DAL.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7_BLL.Services
{
    public interface ISubscriptionService
    {
        Task<UserSubscription?> GetActiveSubscriptionAsync(int userId);
        Task<List<SubscriptionPlan>> GetActivePlansAsync();
        Task<SubscriptionPlan?> GetPlanByIdAsync(int planId);
        Task<bool> UpgradeSubscriptionAsync(int userId, int planId, string? transactionId = null);
        Task<decimal> GetUpgradeAmountAsync(int userId, int targetPlanId);
        Task<bool> CancelAutoRenewAsync(int userId);
        Task<bool> ScheduleDowngradeAsync(int userId, int targetPlanId);
        Task<bool> CancelScheduledDowngradeAsync(int userId);
        Task ProcessExpiredSubscriptionsAsync();
        Task<UserSubscription?> GetPendingSubscriptionAsync(int userId);
    }
}
