namespace Dmb.Model.Dtos;

public class UpdateAdminUserRequestDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? ContactNo { get; set; }
    public string? Address { get; set; }
    public bool Activated { get; set; }
    public bool IsViewable { get; set; }
    public bool IsAdmin { get; set; }
}
