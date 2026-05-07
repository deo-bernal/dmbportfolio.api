namespace Dmb.Model.Dtos;

public class UpdateMyProfileDto
{
    public string? Summary { get; set; }
    public string? Video { get; set; }
    public List<string> Skills { get; set; } = [];
    public UpdateMyProfileContactDto Contact { get; set; } = new();
    public List<UpdateMyProfileProjectCategoryDto> ProjectCategories { get; set; } = [];
}

public class UpdateMyProfileContactDto
{
    public string? Email { get; set; }
    public string? Phone { get; set; }
}

public class UpdateMyProfileProjectCategoryDto
{
    public string Title { get; set; } = string.Empty;
    public List<UpdateMyProfileProjectItemDto> Items { get; set; } = [];
}

public class UpdateMyProfileProjectItemDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
