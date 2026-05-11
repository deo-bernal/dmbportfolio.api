using Dmb.Data.Repository.Interface;
using Dmb.Model.Dtos;
using Dmb.Model.Enums;
using Dmb.Service.Implementation;
using Moq;

namespace dmb.test.Services;

public class AuthServiceTests
{
    private readonly Mock<IAuthRepository> _authRepository = new();
    private readonly Mock<IPasswordResetRepository> _passwordResetRepository = new();

    private AuthService CreateSut() => new(_authRepository.Object, _passwordResetRepository.Object);

    [Fact]
    public async Task LoginWithJwtAsync_DelegatesToRepository()
    {
        var expected = new AuthTokenLoginResult { Status = AuthTokenLoginStatus.Success, AccessToken = "token" };
        _authRepository
            .Setup(x => x.LoginAndIssueJwtAsync(It.IsAny<LoginDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var sut = CreateSut();

        var result = await sut.LoginWithJwtAsync(new LoginDto { Username = "u", Password = "p" });

        Assert.Equal(AuthTokenLoginStatus.Success, result.Status);
        Assert.Equal("token", result.AccessToken);
        _authRepository.Verify(x => x.LoginAndIssueJwtAsync(It.IsAny<LoginDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RequestPasswordResetAsync_DelegatesToPasswordResetRepository()
    {
        _passwordResetRepository
            .Setup(x => x.RequestPasswordResetAsync(It.IsAny<ForgotPasswordDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ForgotPasswordRequestStatus.Ok);
        var sut = CreateSut();

        var result = await sut.RequestPasswordResetAsync(new ForgotPasswordDto { Email = "x@test.com" });

        Assert.Equal(ForgotPasswordRequestStatus.Ok, result);
        _passwordResetRepository.Verify(
            x => x.RequestPasswordResetAsync(It.IsAny<ForgotPasswordDto>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RevokeJtiAsync_DelegatesToAuthRepository()
    {
        var sut = CreateSut();
        var expires = DateTimeOffset.UtcNow.AddHours(1);

        await sut.RevokeJtiAsync("jti-1", expires);

        _authRepository.Verify(x => x.RevokeJtiAsync("jti-1", expires, null, It.IsAny<CancellationToken>()), Times.Once);
    }
}

