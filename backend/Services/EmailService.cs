using System.Net;
using System.Net.Mail;

namespace LuanVanTotNghiep.Services;

public class EmailService(IConfiguration configuration)
{
    public async Task SendOtpEmailAsync(string toEmail, string otp)
    {
        var smtpHost = configuration["Smtp:Host"];
        var smtpPort = int.Parse(configuration["Smtp:Port"] ?? "587");
        var smtpUser = configuration["Smtp:User"];
        var smtpPass = configuration["Smtp:Password"];
        var fromName = configuration["Smtp:FromName"] ?? "Hệ Thống Quản Lý Nhân Viên";

        if (string.IsNullOrWhiteSpace(smtpHost) ||
            string.IsNullOrWhiteSpace(smtpUser) ||
            string.IsNullOrWhiteSpace(smtpPass))
        {
            throw new InvalidOperationException("Missing SMTP configuration. Please set Smtp:Host, Smtp:User, and Smtp:Password.");
        }

        using var client = new SmtpClient(smtpHost, smtpPort)
        {
            Credentials = new NetworkCredential(smtpUser, smtpPass),
            EnableSsl = true
        };

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
}
