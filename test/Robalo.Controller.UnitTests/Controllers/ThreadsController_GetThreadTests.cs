using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Moq;
using Robalo.Common.Extensions;
using Robalo.Common.Models;
using Robalo.Common.Services;
using Robalo.Controller.Api.Controllers;
using Robalo.Controller.Api.Hypermedia.Resources;
using Robalo.Controller.Api.Requests;
using Robalo.Domain.Models;
using Robalo.Domain.Repositories;
using Serilog;

namespace Robalo.Controller.UnitTests.Controllers;

public class ThreadsController_GetThreadTests : ThreadsControllerTests
{
    [Fact]
    public async Task ShouldReturnOK_OnExistingThread()
    {
        var thread = _fixture.Create<ThreadInfo>();

        _repository
            .Setup(r => r.GetThreadInfo(thread.Id, TestContext.Current.CancellationToken))
            .ReturnsAsync(thread);

        var result = await _sut.GetThread(
            id: thread.Id,
            cancellationToken: TestContext.Current.CancellationToken);

        result.ShouldBeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;

        okResult.Value.ShouldBeOfType<ThreadResource>();

        var resource = (ThreadResource)okResult.Value;

        ValidateAllNotNullLinks(resource);

        resource.Links.Events.Href.Query.ShouldBe($"?thread={thread.Id}&checkpoint={thread.Version}");

        resource.CreatedOn.ShouldBe(thread.CreatedOn);
        resource.ModifiedOn.ShouldBe(thread.ModifiedOn);
        resource.Title.ShouldBe(thread.Title);
        resource.ThreadId.ShouldBe(thread.Id);
    }

    [Fact]
    public async Task ShouldReturnNotFound_OnInvalidThreadId()
    {
        _repository.Invocations.Clear();
        var result = await _sut.GetThread(_fixture.Create<string>(), TestContext.Current.CancellationToken);
        result.ShouldBeOfType<NotFoundResult>();
        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldReturnNotFound_OnNonExistingThread()
    {
        _repository.Invocations.Clear();
        var id = ThreadIdentifier.NewIdentifier();
        _repository
            .Setup(r => r.GetThreadInfo(id, TestContext.Current.CancellationToken))
            .ReturnsAsync(None.Default);
        var result = await _sut.GetThread(id, TestContext.Current.CancellationToken);
        result.ShouldBeOfType<NotFoundResult>();
        _repository.Verify(r => r.GetThreadInfo(id, TestContext.Current.CancellationToken), Times.Once());
        _repository.VerifyNoOtherCalls();
    }
}