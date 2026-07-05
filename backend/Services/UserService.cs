using LuanVanTotNghiep.backend.Models.Entities;
using LuanVanTotNghiep.DTOs;
using LuanVanTotNghiep.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace LuanVanTotNghiep.Services;

public class UserService(UserRepo userRepo, AppDbContext context, EmailService emailService)
{
    public async Task<IEnumerable<NsUser>> GetAllUser()
    {
        return await userRepo.GetAll();
    }

    public async Task<NsUser?> GettUserbyId(int id)
    {
        var user = await userRepo.GetbyId(id);
        if (user == null) return null;
        return user;
    }

    public async Task<NsUser> AddUser(UserDto dto)
    {
        var user = new NsUser
        {
            Email = Normalize(dto.Email),
            FullName = dto.FullName,
            PhoneNumber = Normalize(dto.PhoneNumber ?? dto.Phone),
            Password = dto.Password ?? string.Empty,
            BranchId = dto.BranchId,
            RoleId = dto.RoleId,
            HireDate = dto.HireDate
        };
        user.Password = HashPassword(user.Password);
        await userRepo.Add(user);
        await UpsertBankAccountAsync(user.Id, dto.BankName, dto.BankAccountNumber, dto.BankAccountName);
        return user;
    }

    public async Task UpdateUser(UserDto user)
    {
        var us1 = await userRepo.GetbyId(user.Id);
        if (us1 != null)
        {
            if (!string.IsNullOrWhiteSpace(user.Password))
                us1.Password = HashPassword(user.Password);
            us1.Email = Normalize(user.Email);
            us1.FullName = user.FullName;
            us1.PhoneNumber = Normalize(user.PhoneNumber ?? user.Phone);
            us1.BranchId = user.BranchId;
            us1.RoleId = user.RoleId;
            us1.HireDate = user.HireDate;
            await userRepo.Update(us1);
            await UpsertBankAccountAsync(us1.Id, user.BankName, user.BankAccountNumber, user.BankAccountName);
        }
    }

    public async Task DeleteUser(int id)
    {
        var us1 = await userRepo.GetbyId(id);
        if (us1 != null)
        {
            await userRepo.Delete(id);
        }
    }

    public async Task<(bool Success, string Message)> ChangePasswordAsync(int id, string currentPassword, string newPassword, string otp)
    {
        var user = await userRepo.GetbyId(id);
        if (user == null)
            return (false, "Không tìm thấy người dùng.");

        if (!VerifyPassword(currentPassword, user.Password))
            return (false, "Mật khẩu hiện tại không đúng.");

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 4)
            return (false, "Mật khẩu mới cần tối thiểu 4 ký tự.");

        if (!IsValidOtp(user, otp))
            return (false, "Mã OTP không đúng hoặc đã hết hạn.");

        user.Password = HashPassword(newPassword);
        user.ResetPasswordCode = null;
        user.ResetPasswordExpiry = null;
        await userRepo.Update(user);
        return (true, "Đã cập nhật mật khẩu thành công.");
    }

    public async Task<(bool Success, string Message)> SendChangePasswordOtpAsync(int id)
    {
        var user = await userRepo.GetbyId(id);
        if (user == null)
            return (false, "Không tìm thấy người dùng.");

        if (string.IsNullOrWhiteSpace(user.Email))
            return (false, "Tài khoản chưa có email để gửi OTP. Vui lòng cập nhật email trước.");

        await SendOtpAsync(user);
        return (true, "Đã gửi mã OTP về email.");
    }

    public async Task<(bool Success, string Message)> SendPasswordResetOtpAsync(string identifier)
    {
        var user = await FindByIdentifierAsync(identifier);
        if (user == null || string.IsNullOrWhiteSpace(user.Email))
            return (false, "Không tìm thấy tài khoản có email để gửi OTP. Vui lòng kiểm tra email/SĐT hoặc cập nhật email cho nhân viên.");

        await SendOtpAsync(user);

        return (true, "Đã gửi mã OTP về email.");
    }

    public async Task<(bool Success, string Message)> ResetPasswordWithOtpAsync(string identifier, string otp, string newPassword)
    {
        var user = await FindByIdentifierAsync(identifier);
        if (user == null)
            return (false, "Tài khoản hoặc email không tồn tại.");

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 4)
            return (false, "Mật khẩu mới cần tối thiểu 4 ký tự.");

        if (!IsValidOtp(user, otp))
            return (false, "Mã OTP không đúng hoặc đã hết hạn.");

        user.Password = HashPassword(newPassword);
        user.ResetPasswordCode = null;
        user.ResetPasswordExpiry = null;

        await context.SaveChangesAsync();
        return (true, "Đã đặt lại mật khẩu thành công.");
    }

    public static string HashPassword(string plainPassword)
    {
        return global::BCrypt.Net.BCrypt.HashPassword(plainPassword);
    }

    public static bool VerifyPassword(string plainPassword, string storedPassword)
    {
        if (string.IsNullOrEmpty(storedPassword))
            return false;

        if (IsBCryptHash(storedPassword))
            return global::BCrypt.Net.BCrypt.Verify(plainPassword, storedPassword);

        return storedPassword == plainPassword;
    }

    public static bool IsBCryptHash(string password)
    {
        return password.StartsWith("$2a$") || password.StartsWith("$2b$") || password.StartsWith("$2y$");
    }

    public async Task<NsUser?> FindByIdentifierAsync(string identifier)
    {
        var normalized = Normalize(identifier) ?? "";
        var normalizedEmail = normalized.ToLowerInvariant();
        var normalizedPhone = NormalizePhone(normalized);
        var users = await context.NsUsers
            .Where(u => u.Email != null || u.PhoneNumber != null)
            .ToListAsync();

        return users.FirstOrDefault(u =>
            string.Equals(Normalize(u.Email)?.ToLowerInvariant(), normalizedEmail, StringComparison.Ordinal) ||
            NormalizePhone(Normalize(u.PhoneNumber) ?? "") == normalizedPhone);
    }

    private async Task UpsertBankAccountAsync(int userId, string? bankName, string? bankAccountNumber, string? bankAccountName)
    {
        var bank = await context.NsUserBankAccounts.FirstOrDefaultAsync(b => b.UserId == userId);
        var hasBankInfo = !string.IsNullOrWhiteSpace(bankName)
            || !string.IsNullOrWhiteSpace(bankAccountNumber)
            || !string.IsNullOrWhiteSpace(bankAccountName);

        if (bank == null)
        {
            if (!hasBankInfo) return;
            bank = new NsUserBankAccount { UserId = userId };
            context.NsUserBankAccounts.Add(bank);
        }

        bank.BankName = Normalize(bankName);
        bank.BankAccountNumber = Normalize(bankAccountNumber);
        bank.BankAccountName = Normalize(bankAccountName);
        await context.SaveChangesAsync();
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string NormalizePhone(string value)
    {
        return value.Replace(" ", "").Replace("-", "").Replace(".", "").Replace("(", "").Replace(")", "");
    }

    private static string GenerateOtp()
    {
        return RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
    }

    private async Task SendOtpAsync(NsUser user)
    {
        var otp = GenerateOtp();
        user.ResetPasswordCode = otp;
        user.ResetPasswordExpiry = DateTime.UtcNow.AddMinutes(5);
        await context.SaveChangesAsync();
        await emailService.SendOtpEmailAsync(user.Email!, otp);
    }

    private static bool IsValidOtp(NsUser user, string otp)
    {
        return !string.IsNullOrWhiteSpace(user.ResetPasswordCode) &&
            user.ResetPasswordExpiry != null &&
            user.ResetPasswordExpiry >= DateTime.UtcNow &&
            user.ResetPasswordCode == otp?.Trim();
    }
}