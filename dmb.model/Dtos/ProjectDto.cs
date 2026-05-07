namespace Dmb.Model.Dtos;

public class ProjectDto
{
    public int ProjectId { get; set; }
    public int UserId { get; set; }
    public int ProjectTypeId { get; set; }
    public string Name { get; set; } = null!;
    public string? ProjectDetails { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public UserDto? User { get; set; }
    public ProjectTypeDto? ProjectType { get; set; }
}
