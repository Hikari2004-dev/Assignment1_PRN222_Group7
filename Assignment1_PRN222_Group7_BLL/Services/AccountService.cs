using Assignment1_PRN222_Group7_DAL.Entities;
using Assignment1_PRN222_Group7_DAL.Enums;
using Assignment1_PRN222_Group7_DAL.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7_BLL.Services
{
    public class AccountService : IAccountService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AccountService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<User?> LoginAsync(string email, string password)
        {
            var userRepo = _unitOfWork.GetRepository<User>();
            var user = await userRepo.FirstOrDefaultAsync(u => u.Email == email && u.IsActive, "Role");

            if (user == null)
            {
                return null;
            }

            // Kiểm tra mật khẩu bằng BCrypt
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                return null;
            }

            return user;
        }

        public async Task<bool> RegisterAsync(string fullName, string email, string password, int roleId)
        {
            var userRepo = _unitOfWork.GetRepository<User>();

            // Kiểm tra email trùng
            if (await userRepo.AnyAsync(u => u.Email == email))
            {
                return false;
            }

            // Kiểm tra RoleId hợp lệ
            var roleRepo = _unitOfWork.GetRepository<Role>();
            var role = await roleRepo.GetByIdAsync(roleId);
            if (role == null)
            {
                roleId = 1; // fallback Student
            }

            // Tạo user mới
            var user = new User
            {
                FullName     = fullName,
                Email        = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                RoleId       = roleId,
                IsActive     = true,
                CreatedAt    = DateTime.UtcNow
            };

            await userRepo.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            // Tự động đăng ký gói Free
            var planRepo = _unitOfWork.GetRepository<SubscriptionPlan>();
            var freePlan = await planRepo.FirstOrDefaultAsync(p => p.Tier == SubscriptionTier.Free);

            if (freePlan != null)
            {
                var subRepo = _unitOfWork.GetRepository<UserSubscription>();
                await subRepo.AddAsync(new UserSubscription
                {
                    UserId        = user.Id,
                    PlanId        = freePlan.Id,
                    StartDate     = DateTime.UtcNow,
                    IsActive      = true,
                    PaymentStatus = PaymentStatus.Paid
                });
                await _unitOfWork.SaveChangesAsync();
            }

            return true;
        }

        public async Task UpdateLastLoginAsync(int userId)
        {
            var userRepo = _unitOfWork.GetRepository<User>();
            var user = await userRepo.GetByIdAsync(userId);
            if (user != null)
            {
                user.LastLoginAt = DateTime.UtcNow;
                userRepo.Update(user);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        public async Task<List<Role>> GetAvailableRolesAsync()
        {
            var roleRepo = _unitOfWork.GetRepository<Role>();
            // Chỉ lấy Student (Id=1) và Lecturer (Id=2), loại trừ Admin
            var roles = await roleRepo.FindAsync(r => r.Id == 1 || r.Id == 2);
            return roles.ToList();
        }
    }
}
