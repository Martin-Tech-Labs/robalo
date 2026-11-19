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
using Robalo.Domain.Models.Events;
using Robalo.Domain.Repositories;
using Serilog;

namespace Robalo.Controller.UnitTests.Controllers;

public class ThreadsController_GetMessageTests : ThreadsControllerTests
{
    [Theory]
    [InlineData(nameof(UserMessageAdded), false)]
    [InlineData(nameof(AssistantMessageAdded), true)]
    public async Task ShouldReturnOk_OnExistingMessage(string eventType, bool repliedToMessage)
    {
        var id = MessageIdentifier.NewIdentifier();
        var number = _fixture.Create<int>();

        var message = _fixture.Create<Message>() with
        {
            Number = number,
            EventType = eventType,
            RepliedToMessageId = repliedToMessage switch { true => MessageIdentifier.NewIdentifier().AsOption(), false => None.Default }
        };

        _repository
            .Setup(r => r.GetMessage(id, TestContext.Current.CancellationToken))
                .ReturnsAsync(message);

        var result = await _sut.GetMessage(
            id: id,
            cancellationToken: TestContext.Current.CancellationToken);

        result.ShouldBeOfType<OkObjectResult>();

        var okResult = (OkObjectResult)result;
        okResult.Value.ShouldBeOfType<MessageResource>();

        var messageResource = (MessageResource)okResult.Value;

        ValidateAllNotNullLinks(messageResource);

        messageResource.Links.Events.Href.Query.ShouldBe($"?thread={message.ThreadId}&checkpoint={message.Number}");

        if (eventType != nameof(AssistantMessageAdded))
        {
            messageResource.RepliedToMessageId.ShouldBeNull();
            messageResource.Links.RepliedTo.ShouldBeNull();
        }
        else
        {
            messageResource.RepliedToMessageId.ShouldBe(message.RepliedToMessageId.ValueOrFailure);
            messageResource.Links.RepliedTo.ShouldNotBeNull();
        }


        messageResource.MessageId.ShouldBe(message.MessageId);
        messageResource.Content.ShouldBe(message.Content);
        messageResource.CreatedOn.ShouldBe(message.CreatedOn);
        messageResource.EventType.ShouldBe(message.EventType);
        messageResource.MessageNumber.ShouldBe(message.Number);
        messageResource.ThreadId.ShouldBe(message.ThreadId);
    }

    [Fact]
    public async Task ShouldReturnNotFound_OnInvalidMessageId()
    {
        _repository.Invocations.Clear();
        var result = await _sut.GetMessage(_fixture.Create<string>(), TestContext.Current.CancellationToken);
        result.ShouldBeOfType<NotFoundResult>();
        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldReturnNotFound_OnNonExistingMessage()
    {
        _repository.Invocations.Clear();
        var id = MessageIdentifier.NewIdentifier();
        var number = _fixture.Create<int>();
        _repository
            .Setup(r => r.GetMessage(id, TestContext.Current.CancellationToken))
            .ReturnsAsync(None.Default);

        var result = await _sut.GetMessage(id, TestContext.Current.CancellationToken);
        result.ShouldBeOfType<NotFoundResult>();
        _repository.Verify(r => r.GetMessage(id, TestContext.Current.CancellationToken), Times.Once());
    }
}