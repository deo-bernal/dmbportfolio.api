namespace Dmb.Model.Dtos;

/// <summary>
/// Outcome of a login attempt. <see cref="User"/> is set only on success.
/// <see cref="BlockReason"/> is set when credentials are valid but sign-in is not allowed (e.g. not activated).
/// </summary>
public class LoginResult
{
    public LoggedInUserDto? User { get; set; }
    public string? BlockReason { get; set; }
}
