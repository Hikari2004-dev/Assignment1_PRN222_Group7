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
            return await subRepo.FirstOrDefaultAsync(
                us => us.UserId == userId && us.IsActive,
                "Plan"
            );
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

        public async Task<bool> UpgradeSubscriptionAsync(int userId, int planId)
        {
            var planRepo = _unitOfWork.GetRepository<SubscriptionPlan>();
            var plan = await planRepo.FirstOrDefaultAsync(p => p.Id == planId && p.IsActive);

            if (plan == null || plan.Tier == SubscriptionTier.Free)
            {
                return false;
            }

            var subRepo = _unitOfWork.GetRepository<UserSubscription>();

            // 1. Tắt toàn bộ gói cũ đang active của user
            var activeSubs = await subRepo.FindAsync(us => us.UserId == userId && us.IsActive);
            foreach (var sub in activeSubs)
            {
                sub.IsActive = false;
                sub.EndDate = DateTime.UtcNow;
                subRepo.Update(sub);
            }

            // 2. Tạo gói subscription mới
            var newSub = new UserSubscription
            {
                UserId = userId,
                PlanId = plan.Id,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(1), // Gói 1 tháng
                IsActive = true,
                PaymentStatus = PaymentStatus.Paid,
                TransactionId = "MOCK_" + Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper(),
                CreatedAt = DateTime.UtcNow
            };

            await subRepo.AddAsync(newSub);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }
    }
}
