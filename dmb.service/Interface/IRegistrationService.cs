using Dmb.Model.Dtos;
using Dmb.Model.Enums;

namespace Dmb.Service.Interface;

public interface IRegistrationService
{
    Task<RegisterWithActivationOutcome> RegisterWithActivationAsync(
        RegisterDto request,
        CancellationToken cancellationToken = default);

    Task<ActivateAccountOutcome> ActivateAccountAsync(
        string token,
        CancellationToken cancellationToken = default);
}
