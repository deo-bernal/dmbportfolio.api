namespace Dmb.Model.Dtos;

public class ProjectTypeDto
{
    public int ProjectTypeId { get; set; }
    public string TypeName { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
}
