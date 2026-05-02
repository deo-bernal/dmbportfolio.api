namespace Dmb.Service.Interface;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string toEmail, string resetLink, CancellationToken cancellationToken = default);
}
