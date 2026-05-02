namespace Dmb.Model.Dtos;

public class ResetPasswordDto
{
    public required string Token { get; set; }
    public required string NewPassword { get; set; }
    public required string ConfirmPassword { get; set; }
}
