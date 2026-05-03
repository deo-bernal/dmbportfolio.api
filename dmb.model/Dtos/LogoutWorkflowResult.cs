namespace Dmb.Model.Dtos;

public class LogoutWorkflowResult
{
    public string? Username { get; init; }
    public string Message { get; init; } = "Logged out successfully.";
}
