using Dmb.Model.Enums;

namespace Dmb.Model.Dtos;

public class AuthTokenLoginResult
{
    public AuthTokenLoginStatus Status { get; init; }
    public string? AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public bool IsPinSet { get; init; }
    public string? BlockReason { get; init; }
}
