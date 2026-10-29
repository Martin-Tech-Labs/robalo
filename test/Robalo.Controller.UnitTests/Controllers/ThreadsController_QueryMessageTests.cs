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
using Robalo.Domain.Repositories;
using Serilog;

namespace Robalo.Controller.UnitTests.Controllers;

public class ThreadsController_QueryMessageTests : ThreadsControllerTests
{
    [Fact]
    public async Task ShouldReturnOk_OnExistingThread()
    {
        var threadId = ThreadIdentifier.NewIdentifier();
        var cursor = Cursor.Create(22, 5);
        var queryResult = new QueryResult(
            _fixture.CreateMany<Message>(_fixture.Create<int>()).ToList(),
            Cursor.Create(20, 1),
            Cursor.Create(19, 2),
            Cursor.Create(18, 3));

        _repository
            .Setup(r => r.QueryMessages(threadId, cursor, TestContext.Current.CancellationToken))
            .ReturnsAsync(queryResult);

        var result = await _sut.QueryMessages(threadId, cursor, TestContext.Current.CancellationToken);

        VerifyQueryResource(threadId, queryResult, result);
    }

    [Fact]
    public async Task ShouldReturnOk_WithoutQueryCursorString_OnExistingThread()
    {
        var threadId = ThreadIdentifier.NewIdentifier();
        var queryResult = new QueryResult(
            _fixture.CreateMany<Message>(_fixture.Create<int>()).ToList(),
            Cursor.Create(20, 1),
            Cursor.Create(19, 2),
            Cursor.Create(18, 3));

        _repository
            .Setup(r => r.QueryMessages(threadId, Cursor.Last(ThreadsController.QueryLimit), TestContext.Current.CancellationToken))
            .ReturnsAsync(queryResult);

        var result = await _sut.QueryLastMessages(threadId, TestContext.Current.CancellationToken);

        VerifyQueryResource(threadId, queryResult, result);
    }

    private void VerifyQueryResource(ThreadIdentifier threadId, QueryResult queryResult, IActionResult result)
    {
        result.ShouldBeOfType<OkObjectResult>();

        var okResult = (OkObjectResult)result;
        okResult.Value.ShouldBeOfType<MessageQueryResource>();

        var queryResource = (MessageQueryResource)okResult.Value;

        ValidateAllNotNullLinks(queryResource);
        queryResource.Links.Prev.ShouldNotBeNull();
        queryResource.Links.Next.ShouldNotBeNull();

        queryResource.Embedded.ShouldNotBeNull();
        queryResource.Embedded.Messages.ShouldNotBeNull();
        queryResource.Embedded.Messages.Count.ShouldBe(queryResult.Messages.Count);

        for (int i = 0; i < queryResource.Embedded.Messages.Count; i++)
        {
            var messageResource = queryResource.Embedded.Messages[i];
            ValidateAllNotNullLinks(messageResource);
            messageResource.Content.ShouldBe(queryResult.Messages[i].Content);
            messageResource.CreatedOn.ShouldBe(queryResult.Messages[i].CreatedOn);
            messageResource.MessageNumber.ShouldBe(queryResult.Messages[i].Number);
            messageResource.ThreadId.ShouldBe(threadId);
            messageResource.Source.ShouldBe(queryResult.Messages[i].Source.ToString().ToLowerInvariant());
        }
    }

    [Fact]
    public async Task ShouldReturnNotFound_OnNoneQueryResult()
    {
        _repository.Invocations.Clear();

        var threadId = ThreadIdentifier.NewIdentifier();
        var cursor = Cursor.Create(22, 5);

        _repository
            .Setup(r => r.QueryMessages(threadId, cursor, TestContext.Current.CancellationToken))
            .ReturnsAsync(None.Default);

        var result = await _sut.QueryMessages(threadId, cursor, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<NotFoundResult>();

        _repository
            .Verify(r => r.QueryMessages(threadId, cursor, TestContext.Current.CancellationToken), Times.Once());
    }

    [Fact]
    public async Task ShouldReturnOk_OnEmptyQueryResult()
    {
        _repository.Invocations.Clear();

        var threadId = ThreadIdentifier.NewIdentifier();
        var cursor = Cursor.Create(22, 5);

        _repository
            .Setup(r => r.QueryMessages(threadId, cursor, TestContext.Current.CancellationToken))
            .ReturnsAsync(QueryResult.Empty());

        var result = await _sut.QueryMessages(threadId, cursor, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<OkObjectResult>();

        var okResult = (OkObjectResult)result;

        okResult.Value.ShouldBeOfType<MessageQueryResource>();

        var queryResource = (MessageQueryResource)okResult.Value;

        ValidateAllNotNullLinks(queryResource);
        queryResource.Links.Prev.ShouldBeNull();
        queryResource.Links.Next.ShouldBeNull();

        queryResource.Embedded.ShouldNotBeNull();
        (queryResource.Embedded.Messages?.Count ?? 0).ShouldBe(0);
    }

    [Fact]
    public async Task ShouldReturnNotFound_OnInvalidThreadIdentifier()
    {
        _repository.Invocations.Clear();
        var cursor = Cursor.Create(22, 5);
        var result = await _sut.QueryMessages(_fixture.Create<string>(), cursor, TestContext.Current.CancellationToken);
        result.ShouldBeOfType<NotFoundResult>();
        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldReturnNotFound_OnInvalidCursor()
    {
        _repository.Invocations.Clear();
        var result = await _sut.QueryMessages(ThreadIdentifier.NewIdentifier(), _fixture.Create<string>(), TestContext.Current.CancellationToken);
        result.ShouldBeOfType<NotFoundResult>();
        _repository.VerifyNoOtherCalls();
    }
}