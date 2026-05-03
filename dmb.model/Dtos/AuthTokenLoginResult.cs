using Dmb.Model.Enums;

namespace Dmb.Model.Dtos;

public class AuthTokenLoginResult
{
    public AuthTokenLoginStatus Status { get; init; }
    public string? AccessToken { get; init; }
    public string? BlockReason { get; init; }
}
