using Dmb.Model.Abstractions;
using Dmb.Service.Interface;
using Dmb.Service.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Dmb.Service.Implementation;

public class EmailService : IEmailService, IActivationEmailSender, IPasswordResetEmailSender
{
    private readonly SmtpHtmlEmailService _smtpMail;
    private readonly ResendHttpEmailService _resendMail;
    private readonly EmailTemplateProvider _templates;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        SmtpHtmlEmailService smtpMail,
        ResendHttpEmailService resendMail,
        EmailTemplateProvider templates,
        IConfiguration configuration,
        ILogger<EmailService> logger)
    {
        _smtpMail = smtpMail;
        _resendMail = resendMail;
        _templates = templates;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendPasswordResetEmailAsync(
        string toEmail,
        string resetLink,
        CancellationToken cancellationToken = default)
    {
        const string subject = "Password Reset Request";
        var htmlBody = await _templates.GetPasswordResetHtmlAsync(resetLink, cancellationToken);
        await SendByConfiguredProviderAsync(toEmail, subject, htmlBody, cancellationToken);
    }

    public async Task SendAccountActivationEmailAsync(
        string toEmail,
        string activationLink,
        CancellationToken cancellationToken = default)
    {
        const string subject = "Activate your account";
        var htmlBody = await _templates.GetAccountActivationHtmlAsync(activationLink, cancellationToken);
        await SendByConfiguredProviderAsync(toEmail, subject, htmlBody, cancellationToken);
    }

    public async Task SendActivationMonitoringEmailAsync(
        string monitoringEmail,
        string activatedAccountEmail,
        CancellationToken cancellationToken = default)
    {
        const string subject = "New Account Activated";
        var htmlBody = $@"
                <h2>Account activated</h2>
                <p>A new account has completed activation.</p>
                <p><strong>Email:</strong> {System.Net.WebUtility.HtmlEncode(activatedAccountEmail)}</p>
                <p><strong>Activated At (UTC):</strong> {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss}</p>
            ";

        await SendByConfiguredProviderAsync(monitoringEmail, subject, htmlBody, cancellationToken);
    }

    private Task SendByConfiguredProviderAsync(
        string toEmail,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken)
    {
        var provider = (_configuration["Email:Provider"] ?? "smtp").Trim().ToLowerInvariant();
        if (provider == "resend")
        {
            return _resendMail.SendAsync(toEmail, subject, htmlBody, cancellationToken);
        }

        if (provider != "smtp")
        {
            _logger.LogWarning("Unknown Email:Provider '{Provider}', defaulting to SMTP.", provider);
        }
        return _smtpMail.SendAsync(toEmail, subject, htmlBody, cancellationToken);
    }
}
