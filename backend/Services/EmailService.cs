using System.Net;
using System.Net.Mail;

namespace LuanVanTotNghiep.Services;

public class EmailService(IConfiguration configuration)
{
    public async Task SendOtpEmailAsync(string toEmail, string otp)
    {
        var fromName = configuration["Smtp:FromName"] ?? "Hệ Thống Quản Lý Nhân Viên";
        using var client = CreateSmtpClient(out var smtpUser);

        using var mail = new MailMessage
        {
            From = new MailAddress(smtpUser, fromName),
            Subject = "Mã Xác Nhận Đặt Lại Mật Khẩu",
            Body = $"Mã OTP Của Bạn Là : {otp}\nMã có hiệu lực trong 5 phút.",
            IsBodyHtml = false
        };

        mail.To.Add(toEmail);
        await client.SendMailAsync(mail);
    }

    public async Task SendInitialPasswordEmailAsync(
        string toEmail,
        string? fullName,
        string initialPassword)
    {
        var fromName = configuration["Smtp:FromName"] ?? "Hệ Thống Quản Lý Nhân Viên";
        using var client = CreateSmtpClient(out var smtpUser);

        using var mail = new MailMessage
        {
            From = new MailAddress(smtpUser, fromName),
            Subject = "Tài khoản nhân viên đã được tạo",
            Body =
                $"Xin chào {fullName ?? "bạn"},\n\n" +
                "Tài khoản nhân viên của bạn đã được tạo.\n" +
                $"Tên đăng nhập: {toEmail}\n" +
                $"Mật khẩu ban đầu: {initialPassword}\n\n" +
                "Vui lòng đăng nhập và đổi mật khẩu để bảo vệ tài khoản.",
            IsBodyHtml = false
        };

        mail.To.Add(toEmail);
        await client.SendMailAsync(mail);
    }

    private SmtpClient CreateSmtpClient(out string smtpUser)
    {
        var smtpHost = configuration["Smtp:Host"];
        var smtpPort = int.Parse(configuration["Smtp:Port"] ?? "587");
        smtpUser = configuration["Smtp:User"] ?? string.Empty;
        var smtpPass = configuration["Smtp:Password"];

        if (string.IsNullOrWhiteSpace(smtpHost) ||
            string.IsNullOrWhiteSpace(smtpUser) ||
            string.IsNullOrWhiteSpace(smtpPass))
        {
            throw new InvalidOperationException(
                "Missing SMTP configuration. Please set Smtp:Host, Smtp:User, and Smtp:Password.");
        }

        return new SmtpClient(smtpHost, smtpPort)
        {
            Credentials = new NetworkCredential(smtpUser, smtpPass),
            EnableSsl = true
        };
    }
}
