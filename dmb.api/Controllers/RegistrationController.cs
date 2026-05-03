using Dmb.Model.Dtos;
using Dmb.Model.Enums;
using Dmb.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class RegistrationController : ControllerBase
{
    private readonly IRegistrationService _registrationService;

    public RegistrationController(IRegistrationService registrationService)
    {
        _registrationService = registrationService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterDto request, CancellationToken cancellationToken)
    {
        var outcome = await _registrationService.RegisterWithActivationAsync(request, cancellationToken);

        return outcome switch
        {
            RegisterWithActivationOutcome.DuplicateEmail => BadRequest(new { message = "An account with this email already exists." }),
            RegisterWithActivationOutcome.ActivationEmailSendFailed => StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "Unable to send the activation email. Please try again later." }),
            _ => Ok(new { message = "Registration successful. Check your email to activate your account." })
        };
    }

    [HttpPost("activate")]
    [AllowAnonymous]
    public async Task<IActionResult> Activate([FromBody] ActivateAccountDto request, CancellationToken cancellationToken)
    {
        var outcome = await _registrationService.ActivateAccountAsync(request.Token, cancellationToken);

        return outcome switch
        {
            ActivateAccountOutcome.Success => Ok(new { message = "Your account has been activated. You can sign in now." }),
            _ => BadRequest(new { message = "Activation link is invalid or has expired." })
        };
    }
}
