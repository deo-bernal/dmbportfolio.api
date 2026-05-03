namespace Dmb.Model.Dtos;

/// <summary>
/// Registration request. Email is stored as both Email and Username on the User row.
/// </summary>
public class RegisterDto
{
    public required string Email { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Password { get; set; }
    public required string ContactNumber { get; set; }
}
