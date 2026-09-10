using Dmb.Data.Context;
using Dmb.Data.Repository.Interface;
using Dmb.Service.Implementation;
using Dmb.Service.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace dmb.test.Services;

public class ExternalAuthServiceTests
{
    [Fact]
    public void Start_UnknownProvider_ReturnsError()
    {
        var sut = CreateSut();

        var result = sut.Start("twitter", "web", null, "https://api.example/callback");

        Assert.Null(result.RedirectUrl);
        Assert.Equal("Unknown sign-in provider.", result.ErrorMessage);
    }

    [Fact]
    public void Start_UnconfiguredGoogle_RedirectsToLoginError()
    {
        var sut = CreateSut();

        var result = sut.Start("google", "web", "/accent-sidebar", "https://api.example/callback");

        Assert.NotNull(result.RedirectUrl);
        Assert.Contains("/login?ssoError=", result.RedirectUrl, StringComparison.Ordinal);
        Assert.Contains("not configured", Uri.UnescapeDataString(result.RedirectUrl!), StringComparison.OrdinalIgnoreCase);
    }

    private static ExternalAuthService CreateSut()
    {
        var options = new DbContextOptionsBuilder<DmbDbContext>()
            .UseNpgsql("Host=localhost;Database=dmb_test;Username=test;Password=test")
            .Options;
        var db = new DmbDbContext(options);
        var config = new Mock<IConfiguration>();
        config.Setup(c => c[It.IsAny<string>()]).Returns((string key) => key switch
        {
            "Jwt:Secret" => "test-secret-key-for-hmac",
            "App:FrontendUrl" => "https://www.dmbwebsolutions.com",
            _ => null
        });

        return new ExternalAuthService(
            db,
            new Mock<IAuthRepository>().Object,
            new Mock<IEmailService>().Object,
            config.Object,
            new HttpClient(),
            new Mock<ILogger<ExternalAuthService>>().Object);
    }
}
