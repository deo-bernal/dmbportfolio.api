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
}

