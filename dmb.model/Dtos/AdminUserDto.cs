namespace Dmb.Model.Dtos;

public class AdminUserDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? ContactNo { get; set; }
    public string? Address { get; set; }
    public bool Activated { get; set; }
    public bool IsViewable { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsSuperAdmin { get; set; }
    public bool PasswordSet { get; set; } = true;
    public IReadOnlyList<string> LinkedProviders { get; set; } = Array.Empty<string>();
}
