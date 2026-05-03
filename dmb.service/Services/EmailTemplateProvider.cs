using System.Reflection;

namespace Dmb.Service.Services;

/// <summary>
/// Loads HTML fragments from embedded resources built from the project <c>Templates</c> folder.
/// </summary>
public sealed class EmailTemplateProvider
{
    private const string ResourcePrefix = "EmailTemplates/";

    public async Task<string> GetPasswordResetHtmlAsync(string resetLink, CancellationToken cancellationToken = default)
    {
        var html = await ReadTemplateAsync("password-reset.html", cancellationToken);
        return html.Replace("{{ResetLink}}", resetLink, StringComparison.Ordinal);
    }

    public async Task<string> GetAccountActivationHtmlAsync(
        string activationLink,
        CancellationToken cancellationToken = default)
    {
        var html = await ReadTemplateAsync("account-activation.html", cancellationToken);
        return html.Replace("{{ActivationLink}}", activationLink, StringComparison.Ordinal);
    }

    private static async Task<string> ReadTemplateAsync(string fileName, CancellationToken cancellationToken)
    {
        var assembly = typeof(EmailTemplateProvider).Assembly;
        var resourceName = ResourcePrefix + fileName;
        await using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            var known = string.Join(", ", assembly.GetManifestResourceNames() ?? Array.Empty<string>());
            throw new InvalidOperationException(
                $"Embedded email template not found: {resourceName}. Known resources: {known}");
        }

        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
    }
}
