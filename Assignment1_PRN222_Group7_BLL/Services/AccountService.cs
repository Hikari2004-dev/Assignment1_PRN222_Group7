using Assignment1_PRN222_Group7_DAL.Entities;
using Assignment1_PRN222_Group7_DAL.Enums;
using Assignment1_PRN222_Group7_DAL.Repositories;
using Assignment1_PRN222_Group7_BLL.Helpers;
using Assignment1_PRN222_Group7_BLL.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7_BLL.Services
{
    public class AccountService : IAccountService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;

        public AccountService(IUnitOfWork unitOfWork, IEmailService emailService)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
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

        public async Task<bool> RegisterAsync(string fullName, string email, string? password, int roleId, string? studentOrStaffId = null)
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

            // Generate password if null or empty
            var finalPassword = string.IsNullOrWhiteSpace(password) 
                ? Guid.NewGuid().ToString("N").Substring(0, 10) 
                : password;

            // Tạo user mới
            var user = new User
            {
                FullName         = fullName,
                Email            = email,
                PasswordHash     = BCrypt.Net.BCrypt.HashPassword(finalPassword),
                RoleId           = roleId,
                StudentOrStaffId = studentOrStaffId,
                IsActive         = true,
                CreatedAt        = DateTime.UtcNow
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

            // Send email notification in the background
            _ = Task.Run(async () =>
            {
                var roleName = roleId == 2 ? "Lecturer" : (roleId == 3 ? "Admin" : "Student");
                await _emailService.SendAccountCredentialsAsync(user.Email, user.FullName, finalPassword, roleName);
            });

            return true;
        }

        public async Task<BulkRegisterResult> RegisterBulkAsync(Stream excelStream)
        {
            var result = new BulkRegisterResult();
            var rows = ExcelHelper.ReadExcelRows(excelStream);

            if (rows == null || rows.Count == 0)
            {
                result.Errors.Add("Không tìm thấy dữ liệu hợp lệ trong file Excel. Vui lòng kiểm tra lại tiêu đề các cột.");
                result.FailureCount = 0;
                return result;
            }

            var userRepo = _unitOfWork.GetRepository<User>();
            var planRepo = _unitOfWork.GetRepository<SubscriptionPlan>();
            var freePlan = await planRepo.FirstOrDefaultAsync(p => p.Tier == SubscriptionTier.Free);

            int rowIndex = 1; // Row 1 is headers, data starts at Row 2
            foreach (var row in rows)
            {
                rowIndex++;

                row.TryGetValue("FullName", out var fullName);
                row.TryGetValue("Email", out var email);
                row.TryGetValue("Role", out var roleStr);
                row.TryGetValue("StudentOrStaffId", out var studentOrStaffId);

                fullName = fullName?.Trim();
                email = email?.Trim();
                roleStr = roleStr?.Trim().ToLowerInvariant();
                studentOrStaffId = studentOrStaffId?.Trim();

                if (string.IsNullOrWhiteSpace(fullName))
                {
                    result.FailureCount++;
                    result.Errors.Add($"Dòng {rowIndex}: Họ và Tên không được để trống.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(email) || !email.Contains("@") || !email.Contains("."))
                {
                    result.FailureCount++;
                    result.Errors.Add($"Dòng {rowIndex}: Email '{email}' không hợp lệ hoặc để trống.");
                    continue;
                }

                // Check if email already exists
                if (await userRepo.AnyAsync(u => u.Email == email))
                {
                    result.FailureCount++;
                    result.Errors.Add($"Dòng {rowIndex} ({email}): Email đã được sử dụng.");
                    continue;
                }

                // Parse role
                int roleId = 1; // Default to Student (RoleId = 1)
                if (roleStr != null)
                {
                    if (roleStr.Contains("giảng viên") || roleStr.Contains("lecturer") || roleStr.Contains("giang vien"))
                    {
                        roleId = 2;
                    }
                    else if (roleStr.Contains("admin") || roleStr.Contains("quản trị") || roleStr.Contains("quan tri"))
                    {
                        roleId = 3;
                    }
                }

                // Generate random secure password
                var generatedPassword = Guid.NewGuid().ToString("N").Substring(0, 10);

                try
                {
                    var user = new User
                    {
                        FullName = fullName,
                        Email = email,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(generatedPassword),
                        RoleId = roleId,
                        StudentOrStaffId = studentOrStaffId,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    await userRepo.AddAsync(user);
                    await _unitOfWork.SaveChangesAsync();

                    if (freePlan != null)
                    {
                        var subRepo = _unitOfWork.GetRepository<UserSubscription>();
                        await subRepo.AddAsync(new UserSubscription
                        {
                            UserId = user.Id,
                            PlanId = freePlan.Id,
                            StartDate = DateTime.UtcNow,
                            IsActive = true,
                            PaymentStatus = PaymentStatus.Paid
                        });
                        await _unitOfWork.SaveChangesAsync();
                    }

                    result.SuccessCount++;

                    // Send email notification in the background
                    var emailCopy = email;
                    var nameCopy = fullName;
                    var passCopy = generatedPassword;
                    var roleIdCopy = roleId;
                    _ = Task.Run(async () =>
                    {
                        var roleName = roleIdCopy == 2 ? "Lecturer" : (roleIdCopy == 3 ? "Admin" : "Student");
                        await _emailService.SendAccountCredentialsAsync(emailCopy, nameCopy, passCopy, roleName);
                    });
                }
                catch (Exception ex)
                {
                    result.FailureCount++;
                    result.Errors.Add($"Dòng {rowIndex} ({email}): Lỗi hệ thống khi lưu tài khoản. Chi tiết: {ex.Message}");
                }
            }

            return result;
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

        public async Task<List<User>> GetAllUsersAsync()
        {
            var userRepo = _unitOfWork.GetRepository<User>();
            var users = await userRepo.FindAsync(u => true, "Role");
            return users.ToList();
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            var userRepo = _unitOfWork.GetRepository<User>();
            return await userRepo.FirstOrDefaultAsync(u => u.Id == id, "Role");
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            try
            {
                var userRepo = _unitOfWork.GetRepository<User>();
                var existing = await userRepo.GetByIdAsync(user.Id);
                if (existing == null) return false;

                existing.FullName = user.FullName;
                existing.Email = user.Email;
                existing.RoleId = user.RoleId;
                existing.StudentOrStaffId = user.StudentOrStaffId;
                existing.IsActive = user.IsActive;

                if (!string.IsNullOrEmpty(user.PasswordHash))
                {
                    existing.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
                }

                userRepo.Update(existing);
                await _unitOfWork.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ToggleUserStatusAsync(int userId)
        {
            try
            {
                var userRepo = _unitOfWork.GetRepository<User>();
                var user = await userRepo.GetByIdAsync(userId);
                if (user == null) return false;

                user.IsActive = !user.IsActive;
                userRepo.Update(user);
                await _unitOfWork.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<Subject>> GetSubjectsAssignedToLecturerAsync(int lecturerId)
        {
            var assignmentRepo = _unitOfWork.GetRepository<LecturerSubject>();
            var assignments = await assignmentRepo.FindAsync(a => a.LecturerId == lecturerId, "Subject");
            return assignments.Select(a => a.Subject).ToList();
        }

        public async Task<bool> AssignSubjectsToLecturerAsync(int lecturerId, List<int> subjectIds)
        {
            try
            {
                var assignmentRepo = _unitOfWork.GetRepository<LecturerSubject>();
                
                // Remove existing assignments
                var existing = await assignmentRepo.FindAsync(a => a.LecturerId == lecturerId);
                if (existing.Any())
                {
                    assignmentRepo.RemoveRange(existing);
                }

                // Add new assignments
                if (subjectIds != null && subjectIds.Count > 0)
                {
                    var list = subjectIds.Select(sid => new LecturerSubject
                    {
                        LecturerId = lecturerId,
                        SubjectId = sid
                    });
                    await assignmentRepo.AddRangeAsync(list);
                }

                await _unitOfWork.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> IsLecturerAssignedToSubjectAsync(int lecturerId, int subjectId)
        {
            var assignmentRepo = _unitOfWork.GetRepository<LecturerSubject>();
            return await assignmentRepo.AnyAsync(a => a.LecturerId == lecturerId && a.SubjectId == subjectId);
        }
    }
}
