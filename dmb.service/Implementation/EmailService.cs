using System.Net;
using System.Net.Mail;
using Dmb.Service.Interface;
using Microsoft.Extensions.Configuration;

namespace Dmb.Service.Implementation;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink, CancellationToken cancellationToken = default)
    {
        var smtpHost = _config["Email:SmtpHost"]
            ?? throw new InvalidOperationException("Email:SmtpHost is not configured.");
        var smtpPortRaw = _config["Email:SmtpPort"] ?? "587";
        if (!int.TryParse(smtpPortRaw, out var smtpPort))
        {
            throw new InvalidOperationException("Email:SmtpPort must be a valid integer.");
        }

        var smtpUser = _config["Email:SmtpUser"]
            ?? throw new InvalidOperationException("Email:SmtpUser is not configured.");
        var smtpPass = _config["Email:SmtpPass"]
            ?? throw new InvalidOperationException("Email:SmtpPass is not configured.");
        var fromEmail = _config["Email:FromEmail"]
            ?? throw new InvalidOperationException("Email:FromEmail is not configured.");

        using var client = new SmtpClient(smtpHost, smtpPort)
        {
            Credentials = new NetworkCredential(smtpUser, smtpPass),
            EnableSsl = true
        };

        using var mail = new MailMessage
        {
            From = new MailAddress(fromEmail, "Online Profile"),
            Subject = "Password Reset Request",
            Body = $@"
                <h2>Password Reset</h2>
                <p>You requested to reset your password.</p>
                <p>Click the link below to reset it. This link expires in 1 hour.</p>
                <a href='{resetLink}'
                   style='background:#007bff;color:white;padding:10px 20px;
                          text-decoration:none;border-radius:5px;'>
                   Reset Password
                </a>
                <p>If you did not request this, ignore this email.</p>
            ",
            IsBodyHtml = true
        };

        mail.To.Add(toEmail);
        await client.SendMailAsync(mail, cancellationToken);
    }
}
