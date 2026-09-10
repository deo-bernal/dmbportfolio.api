using Dmb.Data.Repository.Interface;
using Dmb.Model.Dtos;
using Dmb.Model.Enums;
using Dmb.Service.Implementation;
using Moq;

namespace dmb.test.Services;

public class DmbReadServiceTests
{
    private readonly Mock<IDmbReadRepository> _repository = new();

    private DmbReadService CreateSut() => new(_repository.Object);

    [Fact]
    public async Task GetMyProfileAsync_ReturnsCanceledStatus_WhenRepositoryThrowsOperationCanceled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _repository
            .Setup(x => x.GetMyProfileByNameIdentifierAsync(It.IsAny<string?>(), cts.Token))
            .ThrowsAsync(new OperationCanceledException(cts.Token));

        var sut = CreateSut();
        var result = await sut.GetMyProfileAsync("1", cts.Token);

        Assert.Equal(MyProfileWorkflowStatus.Canceled, result.Status);
    }

    [Fact]
    public async Task GetPublicResumeAsync_DelegatesToRepository()
    {
        var expected = new ResumeDto
        {
            PersonalInfo = new ResumePersonalInfoDto { FirstName = "Test", LastName = "User" }
        };
        _repository
            .Setup(x => x.GetPublicResumeAsync("john", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var sut = CreateSut();
        var result = await sut.GetPublicResumeAsync("john");

        Assert.NotNull(result);
        Assert.Equal("Test", result!.PersonalInfo.FirstName);
    }

    [Fact]
    public async Task UpsertMyResumeAsync_DelegatesToRepository()
    {
        _repository
            .Setup(x => x.UpsertMyResumeAsync(5, It.IsAny<UpdateResumeDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var sut = CreateSut();

        var ok = await sut.UpsertMyResumeAsync(5, new UpdateResumeDto());

        Assert.True(ok);
        _repository.Verify(
            x => x.UpsertMyResumeAsync(5, It.IsAny<UpdateResumeDto>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TryUpdateAdminUserAsync_DelegatesToRepository()
    {
        var request = new UpdateAdminUserRequestDto
        {
            FirstName = "Deo",
            LastName = "Bernal",
            Email = "deo@example.com"
        };
        _repository
            .Setup(x => x.TryUpdateAdminUserAsync(1, 5, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AdminUserMutationStatus.Ok);
        var sut = CreateSut();

        var status = await sut.TryUpdateAdminUserAsync(1, 5, request);

        Assert.Equal(AdminUserMutationStatus.Ok, status);
    }

    [Fact]
    public async Task TryDeleteAdminUserAsync_DelegatesToRepository()
    {
        _repository
            .Setup(x => x.TryDeleteAdminUserAsync(1, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AdminUserMutationStatus.Ok);
        var sut = CreateSut();

        var status = await sut.TryDeleteAdminUserAsync(1, 5);

        Assert.Equal(AdminUserMutationStatus.Ok, status);
    }

    [Fact]
    public async Task DeleteAccountAsync_DelegatesToRepository()
    {
        _repository
            .Setup(x => x.DeleteAccountAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var sut = CreateSut();

        var ok = await sut.DeleteAccountAsync(5);

        Assert.True(ok);
        _repository.Verify(
            x => x.DeleteAccountAsync(5, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

