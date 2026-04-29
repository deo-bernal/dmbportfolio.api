namespace Dmb.Model.Dtos;

public class UserCompleteDetailsDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? ContactNo { get; set; }
    public bool Activated { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public UserDetailsDto? UserDetails { get; set; }
    public ICollection<ProjectDto> Projects { get; set; } = new List<ProjectDto>();
}
