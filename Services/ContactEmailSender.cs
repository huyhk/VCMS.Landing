using System.Net;
using System.Net.Mail;
using LandingCms.Models;
using Microsoft.Extensions.Options;

namespace LandingCms.Services;

public interface IContactEmailSender
{
    Task SendAsync(ContactSubmission submission, string recipientEmail);
}

public class ContactEmailSender(IOptions<SmtpOptions> options) : IContactEmailSender
{
    public async Task SendAsync(ContactSubmission submission, string recipientEmail)
    {
        var smtp = options.Value;
        if (string.IsNullOrWhiteSpace(smtp.Host) || string.IsNullOrWhiteSpace(smtp.FromEmail) || string.IsNullOrWhiteSpace(recipientEmail))
            throw new InvalidOperationException("SMTP hoặc email nhận liên hệ chưa được cấu hình.");
        using var message = new MailMessage
        {
            From = new MailAddress(smtp.FromEmail, smtp.FromName),
            Subject = $"Liên hệ mới từ {submission.Name}",
            Body = $"Họ tên: {submission.Name}\nEmail: {submission.Email}\nĐiện thoại: {submission.Phone}\n\nNội dung:\n{submission.Message}",
            IsBodyHtml = false
        };
        message.To.Add(new MailAddress(recipientEmail));
        message.ReplyToList.Add(new MailAddress(submission.Email, submission.Name));
        using var client = new SmtpClient(smtp.Host, smtp.Port) { EnableSsl = smtp.EnableSsl };
        if (!string.IsNullOrWhiteSpace(smtp.UserName)) client.Credentials = new NetworkCredential(smtp.UserName, smtp.Password);
        await client.SendMailAsync(message);
    }
}
