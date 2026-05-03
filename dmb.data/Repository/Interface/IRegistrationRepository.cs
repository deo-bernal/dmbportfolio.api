using Dmb.Model.Dtos;
using Dmb.Model.Enums;

namespace Dmb.Data.Repository.Interface;

public interface IRegistrationRepository
{
    Task<RegisterWithActivationOutcome> RegisterWithActivationAsync(
        RegisterDto request,
        CancellationToken cancellationToken = default);

    Task<ActivateAccountOutcome> CompleteAccountActivationAsync(string? token, CancellationToken cancellationToken = default);
}
