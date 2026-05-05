using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Dmb.Service.Services;

/// <summary>
/// Shared SMTP HTML email delivery (common settings and send path for the service layer).
/// </summary>
public sealed class SmtpHtmlEmailService
{
    private const string FromDisplayName = "Online Profile";

    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpHtmlEmailService> _logger;

    public SmtpHtmlEmailService(
        IConfiguration configuration,
        ILogger<SmtpHtmlEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAsync(
        string toEmail,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toEmail);

        var settings = ReadSmtpSettings(_configuration);

        using var client = new SmtpClient(settings.Host, settings.Port)
        {
            Credentials = new NetworkCredential(settings.User, settings.Password),
            EnableSsl = true
        };

        using var mail = new MailMessage
        {
            From = new MailAddress(settings.FromEmail, FromDisplayName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };

        mail.To.Add(toEmail);
        try
        {
            await client.SendMailAsync(mail, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "SMTP send failed. Host: {Host}, Port: {Port}, To: {ToEmail}, Subject: {Subject}",
                settings.Host,
                settings.Port,
                toEmail,
                subject);
            throw;
        }
    }

    private static SmtpSettings ReadSmtpSettings(IConfiguration configuration)
    {
        var host = configuration["Email:SmtpHost"]
            ?? throw new InvalidOperationException("Email:SmtpHost is not configured.");
        var portRaw = configuration["Email:SmtpPort"] ?? "587";
        if (!int.TryParse(portRaw, out var port))
        {
            throw new InvalidOperationException("Email:SmtpPort must be a valid integer.");
        }

        var user = configuration["Email:SmtpUser"]
            ?? throw new InvalidOperationException("Email:SmtpUser is not configured.");
        var password = configuration["Email:SmtpPass"]
            ?? throw new InvalidOperationException("Email:SmtpPass is not configured.");
        var fromEmail = configuration["Email:FromEmail"]
            ?? throw new InvalidOperationException("Email:FromEmail is not configured.");

        return new SmtpSettings(host, port, user, password, fromEmail);
    }

    private readonly record struct SmtpSettings(
        string Host,
        int Port,
        string User,
        string Password,
        string FromEmail);
}
