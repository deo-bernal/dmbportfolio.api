using Dmb.Model.Dtos;

namespace Dmb.Service.Interface;

public interface IExternalAuthService
{
    ExternalAuthStartResult Start(string provider, string? client, string? redirect, string callbackUrl);

    Task<string> HandleCallbackAsync(
        string provider,
        string? code,
        string? state,
        string? error,
        string? errorDescription,
        string callbackUrl,
        CancellationToken cancellationToken = default);

    Task<(bool Ok, string Message, int StatusCode)> CompleteAsync(
        ExternalAuthCompleteRequest request,
        CancellationToken cancellationToken = default);

    Task<ExternalAuthVerifyResult> VerifyAsync(
        ExternalAuthVerifyRequest request,
        CancellationToken cancellationToken = default);
}
