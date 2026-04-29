namespace Dmb.Model.Dtos;

public class UserDetailsDto
{
    public int UserDetailsId { get; set; }
    public int UserId { get; set; }
    public string? Description { get; set; }
    public string? Skills { get; set; }
    public string? Video { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public UserDto? User { get; set; }
}
