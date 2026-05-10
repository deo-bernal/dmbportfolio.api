using Dmb.Data.Repository.Interface;
using Dmb.Model.Dtos;
using Dmb.Model.Enums;
using Dmb.Service.Implementation;
using Moq;

namespace dmb.test.Services;

public class RegistrationServiceTests
{
    private readonly Mock<IRegistrationRepository> _registrationRepository = new();

    private RegistrationService CreateSut() => new(_registrationRepository.Object);

    [Fact]
    public async Task RegisterWithActivationAsync_DelegatesToRepository()
    {
        _registrationRepository
            .Setup(x => x.RegisterWithActivationAsync(It.IsAny<RegisterDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RegisterWithActivationOutcome.Success);
        var sut = CreateSut();

        var result = await sut.RegisterWithActivationAsync(new RegisterDto
        {
            Email = "unit@test.com",
            FirstName = "Unit",
            LastName = "Test",
            Password = "Password79!",
            ContactNumber = "0400000000"
        });

        Assert.Equal(RegisterWithActivationOutcome.Success, result);
    }

    [Fact]
    public async Task ActivateAccountAsync_DelegatesToRepository()
    {
        _registrationRepository
            .Setup(x => x.CompleteAccountActivationAsync("token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(ActivateAccountOutcome.Success);
        var sut = CreateSut();

        var result = await sut.ActivateAccountAsync("token");

        Assert.Equal(ActivateAccountOutcome.Success, result);
        _registrationRepository.Verify(
            x => x.CompleteAccountActivationAsync("token", It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

