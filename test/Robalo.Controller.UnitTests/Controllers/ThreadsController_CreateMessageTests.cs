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
using Robalo.Domain.Models;
using Robalo.Domain.Models.Events;
using Robalo.Domain.Repositories;
using Serilog;

namespace Robalo.Controller.UnitTests.Controllers;

public class ThreadsController_CreateMessageTests : ThreadsControllerTests
{
    [Fact]
    public async Task ShouldReturnCreated_OnExistingThread()
    {
        var request = _fixture.Create<MessageApiRequest>();
        var messageNumber = _fixture.Create<int>();

        var threadId = ThreadIdentifier.NewIdentifier();

        _repository
            .Setup(r => r.AppendEvent(threadId, It.Is<UserMessageAdded>(u => u.Content == request.Content && u.AddedOn == _utcNow), TestContext.Current.CancellationToken))
            .ReturnsAsync(messageNumber);

        var result = await _sut.CreateMessage(threadId, request, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<CreatedResult>();

        var createdResult = (CreatedResult)result;
        createdResult.Value.ShouldBeOfType<MessageResource>();

        var messageResource = (MessageResource)createdResult.Value;

        ValidateAllNotNullLinks(messageResource);

        messageResource.Content.ShouldBe(request.Content);
        messageResource.CreatedOn.ShouldBe(_utcNow);
        messageResource.Source.ShouldBe("user");
        messageResource.MessageNumber.ShouldBe(messageNumber);
        messageResource.ThreadId.ShouldBe(threadId);
    }

    [Fact]
    public async Task ShouldReturnNotFound_OnNoThread()
    {

        var threadId = ThreadIdentifier.NewIdentifier();
        _repository
                   .Setup(r => r.AppendEvent(threadId, It.IsAny<UserMessageAdded>(), TestContext.Current.CancellationToken))
                   .ReturnsAsync(None.Default);

        var result = await _sut.CreateMessage(threadId, _fixture.Create<MessageApiRequest>(), TestContext.Current.CancellationToken);

        result.ShouldBeOfType<NotFoundResult>();
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