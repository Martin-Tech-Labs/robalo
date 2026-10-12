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

public class ThreadsController_GetMessageTests : ThreadsControllerTests
{
    [Fact]
    public async Task ShouldReturnOk_OnExistingMessage()
    {
        var id = ThreadIdentifier.NewIdentifier();
        var number = _fixture.Create<int>();

        var message = _fixture.Create<Message>() with { Number = number };

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

        ValidateAllLinks(messageResource);

        messageResource.Content.ShouldBe(message.Content);
        messageResource.CreatedOn.ShouldBe(message.CreatedOn);
        messageResource.Source.ShouldBe(message.Source.ToString());
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