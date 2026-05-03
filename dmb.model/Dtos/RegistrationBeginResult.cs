using Dmb.Model.Enums;

namespace Dmb.Model.Dtos;

public class RegistrationBeginResult
{
    public RegistrationBeginStatus Status { get; init; }
    public int UserId { get; init; }
    public string Token { get; init; } = string.Empty;
}
