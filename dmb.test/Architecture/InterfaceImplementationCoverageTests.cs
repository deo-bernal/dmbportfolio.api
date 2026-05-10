using Dmb.Data.Repository.Implementation;
using Dmb.Data.Repository.Interface;
using Dmb.Service.Implementation;
using Dmb.Service.Interface;

namespace dmb.test.Architecture;

public class InterfaceImplementationCoverageTests
{
    [Theory]
    [InlineData(typeof(IAuthService), typeof(AuthService))]
    [InlineData(typeof(IDmbReadService), typeof(DmbReadService))]
    [InlineData(typeof(IRegistrationService), typeof(RegistrationService))]
    [InlineData(typeof(IEmailService), typeof(EmailService))]
    [InlineData(typeof(IAuthRepository), typeof(AuthRepository))]
    [InlineData(typeof(IDmbReadRepository), typeof(DmbReadRepository))]
    [InlineData(typeof(IRegistrationRepository), typeof(RegistrationRepository))]
    [InlineData(typeof(IPasswordResetRepository), typeof(PasswordResetRepository))]
    public void Interface_HasConcreteImplementation(Type contract, Type implementation)
    {
        Assert.True(contract.IsInterface);
        Assert.True(contract.IsAssignableFrom(implementation));
    }
}

