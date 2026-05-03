using Dmb.Model.Enums;

namespace Dmb.Model.Dtos;

public class MyProfileWorkflowResult
{
    public MyProfileWorkflowStatus Status { get; init; }
    public UserCompleteDetailsDto? Details { get; init; }
}
