using Dmb.Model.Dtos;
using Dmb.Model.Enums;

namespace Dmb.Data.Repository.Interface;

public interface IPasswordResetRepository
{
    Task<ForgotPasswordRequestStatus> RequestPasswordResetAsync(ForgotPasswordDto request, CancellationToken cancellationToken = default);

    Task<PasswordResetCompletionStatus> CompletePasswordResetAsync(ResetPasswordDto request, CancellationToken cancellationToken = default);
}
