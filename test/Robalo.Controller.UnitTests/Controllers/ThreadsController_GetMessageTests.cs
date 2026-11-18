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
        var id = ThreadIdentifier.NewIdentifier();
        var number = _fixture.Create<int>();

        var message = _fixture.Create<Message>() with
        {
            Number = number,
            EventType = eventType,
            RepliedToMessageId = repliedToMessage switch { true => MessageIdentifier.NewIdentifier().AsOption(), false => None.Default }
        };

        _repository
            .Setup(r => r.GetMessage(id, number, TestContext.Current.CancellationToken))
                .ReturnsAsync(message);

        var result = await _sut.GetMessage(
            id: id,
            number: number,
            cancellationToken: TestContext.Current.CancellationToken);

        result.ShouldBeOfType<OkObjectResult>();

        var okResult = (OkObjectResult)result;
        okResult.Value.ShouldBeOfType<MessageResource>();

        var messageResource = (MessageResource)okResult.Value;

        ValidateAllNotNullLinks(messageResource);

        messageResource.Links.Events.Href.Query.ShouldBe($"?thread={id}&checkpoint={message.Number}");


        messageResource.Content.ShouldBe(message.Content);
        messageResource.CreatedOn.ShouldBe(message.CreatedOn);
        messageResource.EventType.ShouldBe(message.EventType);
        messageResource.MessageNumber.ShouldBe(message.Number);
        messageResource.ThreadId.ShouldBe(id);
    }

    [Fact]
    public async Task ShouldReturnNotFound_OnInvalidThreadId()
    {
        _repository.Invocations.Clear();
        var result = await _sut.GetMessage(_fixture.Create<string>(), _fixture.Create<int>(), TestContext.Current.CancellationToken);
        result.ShouldBeOfType<NotFoundResult>();
        _repository.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ShouldReturnNotFound_OnInvalidMessageNumber(int number)
    {
        _repository.Invocations.Clear();
        var result = await _sut.GetMessage(ThreadIdentifier.NewIdentifier(), number, TestContext.Current.CancellationToken);
        result.ShouldBeOfType<NotFoundResult>();
        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldReturnNotFound_OnNonExistingMessage()
    {
        _repository.Invocations.Clear();
        var id = ThreadIdentifier.NewIdentifier();
        var number = _fixture.Create<int>();
        _repository
            .Setup(r => r.GetMessage(id, number, TestContext.Current.CancellationToken))
            .ReturnsAsync(None.Default);

        var result = await _sut.GetMessage(id, number, TestContext.Current.CancellationToken);
        result.ShouldBeOfType<NotFoundResult>();
        _repository.Verify(r => r.GetMessage(id, number, TestContext.Current.CancellationToken), Times.Once());
    }
}