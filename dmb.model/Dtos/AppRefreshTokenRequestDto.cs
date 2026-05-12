namespace Dmb.Model.Dtos;

public class AppRefreshTokenRequestDto
{
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
}
