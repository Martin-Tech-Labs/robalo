using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Moq;
using Robalo.Common.Models;
using Robalo.Common.Services;
using Robalo.Controller.Api.Controllers;
using Robalo.Controller.Api.Hypermedia.Resources;
using Robalo.Controller.Api.Requests;
using Robalo.Domain.Events;
using Robalo.Domain.Models;
using Robalo.Domain.Repositories;
using Serilog;

namespace Robalo.Controller.UnitTests.Controllers;

public class ThreadsController_GetThreadTests : ThreadsControllerTests
{
    public ThreadsController_GetThreadTests() : base()
    {
    }


    [Fact]
    public async Task ShouldReturnOK_OnExistingThread()
    {
        var modifiedOn = _utcNow.AddSeconds(5);
        var thread =
            Thread.NewThread(ThreadIdentifier.NewIdentifier(), _utcNow)
            .AddUserMessage(_fixture.Create<string>(), modifiedOn)
            .UpdateTitle(_fixture.Create<string>())
            .MarkAggregateAsSynchronized();

        _repository
            .Setup(r => r.GetThread(thread.Id, TestContext.Current.CancellationToken))
            .ReturnsAsync(thread);

        var result = await _sut.GetThread(
            id: thread.Id,
            cancellationToken: TestContext.Current.CancellationToken);

        result.ShouldBeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;

        okResult.Value.ShouldBeOfType<ThreadResource>();

        var resource = (ThreadResource)okResult.Value;

        ValidateAllLinks(resource);

        resource.CreatedOn.ShouldBe(_utcNow);
        resource.ModifiedOn.ShouldBe(modifiedOn);
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
        var result = await _sut.GetThread(id, TestContext.Current.CancellationToken);
        result.ShouldBeOfType<NotFoundResult>();
        _repository.Verify(r => r.GetThread(id, TestContext.Current.CancellationToken), Times.Once());
    }
}