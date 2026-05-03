using Dmb.Model.Abstractions;
using Dmb.Service.Interface;
using Dmb.Service.Services;

namespace Dmb.Service.Implementation;

public class EmailService : IEmailService, IActivationEmailSender, IPasswordResetEmailSender
{
    private readonly SmtpHtmlEmailService _smtpMail;
    private readonly EmailTemplateProvider _templates;

    public EmailService(SmtpHtmlEmailService smtpMail, EmailTemplateProvider templates)
    {
        _smtpMail = smtpMail;
        _templates = templates;
    }

    public async Task SendPasswordResetEmailAsync(
        string toEmail,
        string resetLink,
        CancellationToken cancellationToken = default)
    {
        const string subject = "Password Reset Request";
        var htmlBody = await _templates.GetPasswordResetHtmlAsync(resetLink, cancellationToken);
        await _smtpMail.SendAsync(toEmail, subject, htmlBody, cancellationToken);
    }

    public async Task SendAccountActivationEmailAsync(
        string toEmail,
        string activationLink,
        CancellationToken cancellationToken = default)
    {
        const string subject = "Activate your account";
        var htmlBody = await _templates.GetAccountActivationHtmlAsync(activationLink, cancellationToken);
        await _smtpMail.SendAsync(toEmail, subject, htmlBody, cancellationToken);
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

        await _smtpMail.SendAsync(monitoringEmail, subject, htmlBody, cancellationToken);
    }
}
