using LuanVanTotNghiep.backend.Models.Entities;
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

    public async Task AddUser(NsUser user)
    {
        user.Password = HashPassword(user.Password);
        await userRepo.Add(user);
    }

    public async Task UpdateUser(NsUser user)
    {
        var us1 = await userRepo.GetbyId(user.Id);
        if (us1 != null)
        {
            us1.Username = user.Username;
            if (!string.IsNullOrWhiteSpace(user.Password))
                us1.Password = HashPassword(user.Password);
            us1.FullName = user.FullName;
            us1.PhoneNumber = user.PhoneNumber;
            us1.BankName = user.BankName;
            us1.BankAccountNumber = user.BankAccountNumber;
            us1.BankAccountName = user.BankAccountName;
            us1.BranchId = user.BranchId;
            us1.RoleId = user.RoleId;
            us1.HireDate = user.HireDate;
            await userRepo.Update(us1);
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

    public async Task<(bool Success, string Message)> ChangePasswordAsync(int id, string currentPassword, string newPassword)
    {
        var user = await userRepo.GetbyId(id);
        if (user == null)
            return (false, "Khong tim thay nguoi dung.");

        if (!VerifyPassword(currentPassword, user.Password))
            return (false, "Mat khau hien tai khong dung.");

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 4)
            return (false, "Mat khau moi can toi thieu 4 ky tu.");

        user.Password = HashPassword(newPassword);
        await userRepo.Update(user);
        return (true, "Da cap nhat mat khau thanh cong.");
    }

    public async Task<(bool Success, string Message)> SendPasswordResetOtpAsync(string identifier)
    {
        var user = await FindByIdentifierAsync(identifier);
        if (user == null || string.IsNullOrWhiteSpace(user.Email))
            return (false, "Không tìm thấy email của tài khoản.");

        var otp = GenerateOtp();
        user.ResetPasswordCode = otp;
        user.ResetPasswordExpiry = DateTime.UtcNow.AddMinutes(5);

        await context.SaveChangesAsync();
        await emailService.SendOtpEmailAsync(user.Email, otp);

        return (true, "Đã gửi mã OTP về email.");
    }

    public async Task<(bool Success, string Message)> ResetPasswordWithOtpAsync(string identifier, string otp, string newPassword)
    {
        var user = await FindByIdentifierAsync(identifier);
        if (user == null)
            return (false, "Tai khoan hoac email khong ton tai.");

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 4)
            return (false, "Mat khau moi can toi thieu 4 ky tu.");

        if (string.IsNullOrWhiteSpace(user.ResetPasswordCode) ||
            user.ResetPasswordExpiry == null ||
            user.ResetPasswordExpiry < DateTime.UtcNow ||
            user.ResetPasswordCode != otp)
        {
            return (false, "Ma OTP khong dung hoac da het han.");
        }

        user.Password = HashPassword(newPassword);
        user.ResetPasswordCode = null;
        user.ResetPasswordExpiry = null;

        await context.SaveChangesAsync();
        return (true, "Da dat lai mat khau thanh cong.");
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

    private async Task<NsUser?> FindByIdentifierAsync(string identifier)
    {
        var normalized = identifier.Trim();
        return await context.NsUsers.FirstOrDefaultAsync(u =>
            u.Username == normalized || u.Email == normalized);
    }

    private static string GenerateOtp()
    {
        return RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
    }
}
