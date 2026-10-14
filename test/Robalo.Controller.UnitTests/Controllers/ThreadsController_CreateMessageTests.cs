using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Diagnostics.Tracing;
using Moq;
using Robalo.Common.Extensions;
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

public class ThreadsController_CreateMessageTests : ThreadsControllerTests
{
    [Fact]
    public async Task ShouldReturnCreated_OnExistingThread()
    {
        var thread = Thread.NewThread(ThreadIdentifier.NewIdentifier(), _utcNow.AddSeconds(-5))
            .ForEach(
                Enumerable.Range(0, _fixture.Create<int>()),
                (t, _) => t.AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(-2)))
            .MarkAggregateAsSynchronized();

        var request = _fixture.Create<MessageApiRequest>();

        _repository
            .Setup(r => r.GetThread(thread.Id, TestContext.Current.CancellationToken))
            .ReturnsAsync(thread);

        _repository
            .Setup(r => r.SaveThread(thread, TestContext.Current.CancellationToken))
            .ReturnsAsync(new Updated());


        var result = await _sut.CreateMessage(thread.Id, request, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<CreatedResult>();

        var createdResult = (CreatedResult)result;
        createdResult.Value.ShouldBeOfType<MessageResource>();

        var messageResource = (MessageResource)createdResult.Value;

        ValidateAllNotNullLinks(messageResource);

        messageResource.Content.ShouldBe(request.Content);
        messageResource.CreatedOn.ShouldBe(_utcNow);
        messageResource.Source.ShouldBe("user");
        messageResource.MessageNumber.ShouldBe(thread.Messages.Count);
        messageResource.ThreadId.ShouldBe(thread.Id);

        _repository.Verify(r => r.SaveThread(It.Is<Thread>(
            t => t.Id == thread.Id &&
            t.PendingEvents.Single().GetType() == typeof(UserMessageAdded) &&
            ((UserMessageAdded)t.PendingEvents.Single()).Content == request.Content &&
            ((UserMessageAdded)t.PendingEvents.Single()).AddedOn == _utcNow &&
            t.Messages[t.Messages.Count - 1].Content == request.Content &&
            t.Messages[t.Messages.Count - 1].CreatedOn == _utcNow &&
            t.Messages[t.Messages.Count - 1].Source == MessageSource.User &&
            t.Messages[t.Messages.Count - 1].Number == t.Messages.Count),
            TestContext.Current.CancellationToken), Times.Once());
    }

    [Fact]
    public async Task ShouldReturnConflict_OnConcurrencyConflict()
    {
        var thread = Thread.NewThread(ThreadIdentifier.NewIdentifier(), _utcNow).MarkAggregateAsSynchronized();

        _repository
            .Setup(r => r.GetThread(thread.Id, TestContext.Current.CancellationToken))
            .ReturnsAsync(thread);

        _repository
            .Setup(r => r.SaveThread(thread, TestContext.Current.CancellationToken))
            .ReturnsAsync(new ConcurrencyConflict());

        var result = await _sut.CreateMessage(thread.Id, _fixture.Create<MessageApiRequest>(), TestContext.Current.CancellationToken);

        result.ShouldBeOfType<ConflictObjectResult>();

        var conflictResult = (ConflictObjectResult)result;

        conflictResult.Value.ShouldBeOfType<ProblemDetails>();

        _problemDetailsFactory.Verify(v => v.CreateProblemDetails(
            It.IsAny<HttpContext>(), 409, "Conflict", It.IsAny<string>(), "thread_updated_in_parallel", It.IsAny<string>()), Times.Once());
    }

    [Fact]
    public async Task ShouldThrow_OnCreatedResultFromRepository()
    {
        var thread = Thread.NewThread(ThreadIdentifier.NewIdentifier(), _utcNow).MarkAggregateAsSynchronized();

        _repository
            .Setup(r => r.GetThread(thread.Id, TestContext.Current.CancellationToken))
            .ReturnsAsync(thread);

        _repository
            .Setup(r => r.SaveThread(thread, TestContext.Current.CancellationToken))
            .ReturnsAsync(new Created());

        await Should.ThrowAsync<InvalidOperationException>(() => _sut.CreateMessage(thread.Id, _fixture.Create<MessageApiRequest>(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ShouldThrow_OnNoChangedResultFromRepository()
    {
        var thread = Thread.NewThread(ThreadIdentifier.NewIdentifier(), _utcNow).MarkAggregateAsSynchronized();

        _repository
            .Setup(r => r.GetThread(thread.Id, TestContext.Current.CancellationToken))
            .ReturnsAsync(thread);

        _repository
            .Setup(r => r.SaveThread(thread, TestContext.Current.CancellationToken))
            .ReturnsAsync(new NoChanges());

        await Should.ThrowAsync<InvalidOperationException>(() => _sut.CreateMessage(thread.Id, _fixture.Create<MessageApiRequest>(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ShouldReturnNotFound_OnNonExistingThread()
    {
        _repository.Invocations.Clear();

        var id = ThreadIdentifier.NewIdentifier();
        _repository
            .Setup(r => r.GetThread(id, TestContext.Current.CancellationToken))
            .ReturnsAsync(None.Default);

        var result = await _sut.CreateMessage(id, _fixture.Create<MessageApiRequest>(), TestContext.Current.CancellationToken);

        result.ShouldBeOfType<NotFoundResult>();

        _repository.Verify(r => r.GetThread(id, TestContext.Current.CancellationToken), Times.Once());
        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldReturnNotFound_OnInvalidIdentifier()
    {
        _repository.Invocations.Clear();
        var result = await _sut.CreateMessage(_fixture.Create<string>(), _fixture.Create<MessageApiRequest>(), TestContext.Current.CancellationToken);
        result.ShouldBeOfType<NotFoundResult>();
        _repository.VerifyNoOtherCalls();
    }
}