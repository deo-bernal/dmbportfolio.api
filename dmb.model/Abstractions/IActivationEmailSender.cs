namespace Dmb.Model.Abstractions;

/// <summary>
/// Used by the data layer for activation-related emails without taking a dependency on the service assembly.
/// </summary>
public interface IActivationEmailSender
{
    Task SendAccountActivationEmailAsync(string toEmail, string activationLink, CancellationToken cancellationToken = default);

    Task SendActivationMonitoringEmailAsync(
        string monitoringEmail,
        string activatedAccountEmail,
        CancellationToken cancellationToken = default);
}
