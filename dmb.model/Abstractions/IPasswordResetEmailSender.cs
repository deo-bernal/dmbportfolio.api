namespace Dmb.Model.Abstractions;

public interface IPasswordResetEmailSender
{
    Task SendPasswordResetEmailAsync(string toEmail, string resetLink, CancellationToken cancellationToken = default);
}
