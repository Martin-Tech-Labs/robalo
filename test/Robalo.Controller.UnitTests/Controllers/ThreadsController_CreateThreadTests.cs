using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Moq;
using Robalo.Common.Models;
using Robalo.Common.Services;
using Robalo.Controller.Api.Controllers;
using Robalo.Controller.Api.Hypermedia.Resources;
using Robalo.Controller.Api.Requests;
using Robalo.Domain.Models.Events;
using Robalo.Domain.Repositories;
using Serilog;

namespace Robalo.Controller.UnitTests.Controllers;

public class ThreadsController_CreateThreadTests : ThreadsControllerTests
{
    [Fact]
    public async Task ShouldReturnCreated_OnCreatedThread()
    {
        _repository.Invocations.Clear();

        var request = _fixture.Create<MessageApiRequest>();

        _repository
            .Setup(r => r.SaveThread(It.IsAny<Thread>(), It.IsAny<CancellationToken>()))
            .Callback<Thread, CancellationToken>((t, _) => t.MarkAggregateAsSynchronized())
            .ReturnsAsync(new Created());

        var result = await _sut.CreateThread(
            request: request,
            cancellationToken: TestContext.Current.CancellationToken);

        result.ShouldBeOfType<CreatedResult>();
        var createdResult = (CreatedResult)result;

        createdResult.Value.ShouldBeOfType<ThreadResource>();

        var resource = (ThreadResource)createdResult.Value;

        ValidateAllNotNullLinks(resource);

        createdResult.Location.ShouldBe(resource.Links.Self.Href.AbsoluteUri);
        resource.CreatedOn.ShouldBe(_utcNow);
        resource.ModifiedOn.ShouldBe(_utcNow);
        resource.Title.ShouldBeNullOrWhiteSpace();
        resource.ThreadId.ShouldNotBeNullOrWhiteSpace();

        _repository
            .Verify(r => r.SaveThread(It.Is<Thread>(
                t => t.CreatedOn == _utcNow &&
                t.Messages.Count == 1 &&
                t.Title == null &&
                t.Messages[0].Content == request.Content &&
                t.Messages[0].CreatedOn == _utcNow &&
                t.Id == resource.ThreadId), TestContext.Current.CancellationToken),
            Times.Once());
    }


    [Fact]
    public async Task ShouldThrow_OnRepositoryConcurrencyConflictResult()
    {
        _repository
            .Setup(r => r.SaveThread(It.IsAny<Thread>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConcurrencyConflict());

        await Should.ThrowAsync<InvalidOperationException>(() => _sut.CreateThread(
            _fixture.Create<MessageApiRequest>(),
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ShouldThrow_OnRepositoryUpdatedResult()
    {
        _repository
            .Setup(r => r.SaveThread(It.IsAny<Thread>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Updated());

        await Should.ThrowAsync<InvalidOperationException>(() => _sut.CreateThread(
            _fixture.Create<MessageApiRequest>(),
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ShouldThrow_OnRepositoryNoChangesResult()
    {
        _repository
            .Setup(r => r.SaveThread(It.IsAny<Thread>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NoChanges());

        await Should.ThrowAsync<InvalidOperationException>(() => _sut.CreateThread(
            _fixture.Create<MessageApiRequest>(),
            TestContext.Current.CancellationToken));
    }
}