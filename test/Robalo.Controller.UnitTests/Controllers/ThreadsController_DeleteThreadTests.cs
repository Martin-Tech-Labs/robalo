using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Moq;
using Robalo.Common.Models;
using Robalo.Common.Services;
using Robalo.Controller.Api.Controllers;
using Robalo.Controller.Api.Hypermedia.Resources;
using Robalo.Controller.Api.Requests;
using Robalo.Domain.Models;
using Robalo.Domain.Repositories;
using Serilog;

namespace Robalo.Controller.UnitTests.Controllers;

public class ThreadsController_DeleteThreadTests : ThreadsControllerTests
{
    [Fact]
    public async Task ShouldReturnNoContent_OnExistingThread()
    {
        var id = ThreadIdentifier.NewIdentifier();

        _repository
            .Setup(r => r.DeleteThread(id, TestContext.Current.CancellationToken))
            .ReturnsAsync(new Success());

        var result = await _sut.DeleteThread(
            id: id,
            cancellationToken: TestContext.Current.CancellationToken);

        result.ShouldBeOfType<NoContentResult>();
    }

    [Fact]
    public async Task ShouldReturnNotFound_OnInvalidThreadId()
    {
        _repository.Invocations.Clear();
        var result = await _sut.DeleteThread(_fixture.Create<string>(), TestContext.Current.CancellationToken);
        result.ShouldBeOfType<NotFoundResult>();
        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldReturnNotFound_OnNonExistingThread()
    {
        _repository.Invocations.Clear();
        var id = ThreadIdentifier.NewIdentifier();
        _repository
            .Setup(r => r.DeleteThread(id, TestContext.Current.CancellationToken))
            .ReturnsAsync(new NotFound());

        var result = await _sut.DeleteThread(id, TestContext.Current.CancellationToken);
        result.ShouldBeOfType<NotFoundResult>();
        _repository.Verify(r => r.DeleteThread(id, TestContext.Current.CancellationToken), Times.Once());
    }
}