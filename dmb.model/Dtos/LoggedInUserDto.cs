namespace Dmb.Model.Dtos;

public class LoggedInUserDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public bool Activated { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
