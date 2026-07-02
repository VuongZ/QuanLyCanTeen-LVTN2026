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
        var fromName = configuration["Smtp:FromName"] ?? "He thong quan ly nhan vien";

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
            Subject = "Ma xac nhan dat lai mat khau",
            Body = $"Ma OTP cua ban la: {otp}\nMa co hieu luc trong 5 phut.",
            IsBodyHtml = false
        };

        mail.To.Add(toEmail);
        await client.SendMailAsync(mail);
    }
}
