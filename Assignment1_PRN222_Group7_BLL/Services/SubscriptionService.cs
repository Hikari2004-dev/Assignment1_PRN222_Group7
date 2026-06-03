using Assignment1_PRN222_Group7_DAL.Entities;
using Assignment1_PRN222_Group7_DAL.Enums;
using Assignment1_PRN222_Group7_DAL.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7_BLL.Services
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly IUnitOfWork _unitOfWork;

        public SubscriptionService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<UserSubscription?> GetActiveSubscriptionAsync(int userId)
        {
            var subRepo = _unitOfWork.GetRepository<UserSubscription>();
            var sub = await subRepo.FirstOrDefaultAsync(
                us => us.UserId == userId && us.IsActive,
                "Plan"
            );

            if (sub != null && sub.EndDate.HasValue && sub.EndDate.Value < DateTime.UtcNow)
            {
                sub.IsActive = false;
                sub.PaymentStatus = PaymentStatus.Expired;
                subRepo.Update(sub);

                if (sub.AutoRenew && sub.ScheduledPlanId.HasValue)
                {
                    var pendingSub = new UserSubscription
                    {
                        UserId = sub.UserId,
                        PlanId = sub.ScheduledPlanId.Value,
                        StartDate = sub.EndDate ?? DateTime.UtcNow,
                        EndDate = (sub.EndDate ?? DateTime.UtcNow).AddMonths(1),
                        IsActive = false,
                        PaymentStatus = PaymentStatus.Pending,
                        CreatedAt = DateTime.UtcNow
                    };
                    await subRepo.AddAsync(pendingSub);
                }

                await _unitOfWork.SaveChangesAsync();
                return null;
            }

            return sub;
        }

        public async Task<List<SubscriptionPlan>> GetActivePlansAsync()
        {
            var planRepo = _unitOfWork.GetRepository<SubscriptionPlan>();
            var plans = await planRepo.FindAsync(p => p.IsActive);
            return plans.OrderBy(p => p.Price).ToList();
        }

        public async Task<SubscriptionPlan?> GetPlanByIdAsync(int planId)
        {
            var planRepo = _unitOfWork.GetRepository<SubscriptionPlan>();
            return await planRepo.FirstOrDefaultAsync(p => p.Id == planId && p.IsActive);
        }

        public async Task<bool> UpgradeSubscriptionAsync(int userId, int planId, string? transactionId = null)
        {
            var planRepo = _unitOfWork.GetRepository<SubscriptionPlan>();
            var plan = await planRepo.FirstOrDefaultAsync(p => p.Id == planId && p.IsActive);

            if (plan == null || plan.Tier == SubscriptionTier.Free)
            {
                return false;
            }

            var subRepo = _unitOfWork.GetRepository<UserSubscription>();

            // Kiểm tra xem có gói pending nào không
            var pendingSub = await subRepo.FirstOrDefaultAsync(
                us => us.UserId == userId && us.PlanId == planId && us.PaymentStatus == PaymentStatus.Pending
            );

            if (pendingSub != null)
            {
                // Tắt toàn bộ gói cũ đang active của user
                var activeSubs = await subRepo.FindAsync(us => us.UserId == userId && us.IsActive);
                foreach (var sub in activeSubs)
                {
                    sub.IsActive = false;
                    sub.EndDate = DateTime.UtcNow;
                    subRepo.Update(sub);
                }

                pendingSub.IsActive = true;
                pendingSub.PaymentStatus = PaymentStatus.Paid;
                pendingSub.StartDate = DateTime.UtcNow;
                pendingSub.EndDate = DateTime.UtcNow.AddMonths(1);
                pendingSub.TransactionId = transactionId ?? ("MOCK_" + Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper());
                subRepo.Update(pendingSub);

                await _unitOfWork.SaveChangesAsync();
                return true;
            }

            // Tắt toàn bộ gói cũ đang active của user
            var activeSubsList = await subRepo.FindAsync(us => us.UserId == userId && us.IsActive);
            foreach (var sub in activeSubsList)
            {
                sub.IsActive = false;
                sub.EndDate = DateTime.UtcNow;
                subRepo.Update(sub);
            }

            // Tạo gói subscription mới
            var newSub = new UserSubscription
            {
                UserId = userId,
                PlanId = plan.Id,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(1),
                IsActive = true,
                PaymentStatus = PaymentStatus.Paid,
                TransactionId = transactionId ?? ("MOCK_" + Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper()),
                CreatedAt = DateTime.UtcNow,
                AutoRenew = true
            };

            await subRepo.AddAsync(newSub);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<decimal> GetUpgradeAmountAsync(int userId, int targetPlanId)
        {
            var planRepo = _unitOfWork.GetRepository<SubscriptionPlan>();
            var targetPlan = await planRepo.FirstOrDefaultAsync(p => p.Id == targetPlanId && p.IsActive);
            if (targetPlan == null) return 0;

            var activeSub = await GetActiveSubscriptionAsync(userId);
            if (activeSub == null || activeSub.Plan.Tier == SubscriptionTier.Free)
            {
                return targetPlan.Price;
            }

            // Basic -> Premium nâng cấp ngay
            if (activeSub.Plan.Tier == SubscriptionTier.Basic && targetPlan.Tier == SubscriptionTier.Premium)
            {
                if (activeSub.EndDate.HasValue && activeSub.EndDate.Value > DateTime.UtcNow)
                {
                    double remainingDays = (activeSub.EndDate.Value - DateTime.UtcNow).TotalDays;
                    if (remainingDays > 0)
                    {
                        decimal dailyValue = activeSub.Plan.Price / 30.0m;
                        decimal refundValue = dailyValue * (decimal)remainingDays;
                        decimal upgradeCost = targetPlan.Price - refundValue;
                        return upgradeCost < 0 ? 0 : Math.Round(upgradeCost, 0);
                    }
                }
            }

            return targetPlan.Price;
        }

        public async Task<bool> CancelAutoRenewAsync(int userId)
        {
            var subRepo = _unitOfWork.GetRepository<UserSubscription>();
            var activeSub = await subRepo.FirstOrDefaultAsync(us => us.UserId == userId && us.IsActive);
            if (activeSub == null) return false;

            activeSub.AutoRenew = false;
            activeSub.ScheduledPlanId = null;
            subRepo.Update(activeSub);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ScheduleDowngradeAsync(int userId, int targetPlanId)
        {
            var planRepo = _unitOfWork.GetRepository<SubscriptionPlan>();
            var targetPlan = await planRepo.FirstOrDefaultAsync(p => p.Id == targetPlanId && p.IsActive);
            if (targetPlan == null || targetPlan.Tier != SubscriptionTier.Basic) return false;

            var subRepo = _unitOfWork.GetRepository<UserSubscription>();
            var activeSub = await subRepo.FirstOrDefaultAsync(us => us.UserId == userId && us.IsActive, "Plan");
            if (activeSub == null || activeSub.Plan.Tier != SubscriptionTier.Premium) return false;

            activeSub.ScheduledPlanId = targetPlanId;
            activeSub.AutoRenew = true;
            subRepo.Update(activeSub);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CancelScheduledDowngradeAsync(int userId)
        {
            var subRepo = _unitOfWork.GetRepository<UserSubscription>();
            var activeSub = await subRepo.FirstOrDefaultAsync(us => us.UserId == userId && us.IsActive);
            if (activeSub == null) return false;

            activeSub.ScheduledPlanId = null;
            subRepo.Update(activeSub);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<UserSubscription?> GetPendingSubscriptionAsync(int userId)
        {
            var subRepo = _unitOfWork.GetRepository<UserSubscription>();
            return await subRepo.FirstOrDefaultAsync(
                us => us.UserId == userId && !us.IsActive && us.PaymentStatus == PaymentStatus.Pending,
                "Plan"
            );
        }

        public async Task ProcessExpiredSubscriptionsAsync()
        {
            var subRepo = _unitOfWork.GetRepository<UserSubscription>();
            var expiredSubs = await subRepo.FindAsync(
                us => us.IsActive && us.EndDate.HasValue && us.EndDate.Value < DateTime.UtcNow,
                "Plan"
            );

            foreach (var sub in expiredSubs)
            {
                sub.IsActive = false;
                sub.PaymentStatus = PaymentStatus.Expired;
                subRepo.Update(sub);

                if (sub.AutoRenew && sub.ScheduledPlanId.HasValue)
                {
                    var pendingSub = new UserSubscription
                    {
                        UserId = sub.UserId,
                        PlanId = sub.ScheduledPlanId.Value,
                        StartDate = sub.EndDate ?? DateTime.UtcNow,
                        EndDate = (sub.EndDate ?? DateTime.UtcNow).AddMonths(1),
                        IsActive = false,
                        PaymentStatus = PaymentStatus.Pending,
                        CreatedAt = DateTime.UtcNow
                    };
                    await subRepo.AddAsync(pendingSub);
                }
            }

            if (expiredSubs.Any())
            {
                await _unitOfWork.SaveChangesAsync();
            }
        }
    }
}
