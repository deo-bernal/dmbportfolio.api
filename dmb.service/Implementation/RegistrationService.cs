using Dmb.Data.Repository.Interface;
using Dmb.Model.Dtos;
using Dmb.Model.Enums;
using Dmb.Service.Interface;

namespace Dmb.Service.Implementation;

public class RegistrationService : IRegistrationService
{
    private readonly IRegistrationRepository _registrationRepository;

    public RegistrationService(IRegistrationRepository registrationRepository)
    {
        _registrationRepository = registrationRepository;
    }

    public Task<RegisterWithActivationOutcome> RegisterWithActivationAsync(
        RegisterDto request,
        CancellationToken cancellationToken = default)
    {
        return _registrationRepository.RegisterWithActivationAsync(request, cancellationToken);
    }

    public Task<ActivateAccountOutcome> ActivateAccountAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        return _registrationRepository.CompleteAccountActivationAsync(token, cancellationToken);
    }
}
