using Moq;
using Robalo.Common.Models;
using Robalo.Common.Services;
using Robalo.Domain.Clients;
using Robalo.Domain.Clients.Models;
using Robalo.Domain.Events;
using Robalo.Domain.Models;
using Robalo.Domain.Repositories;
using Robalo.Domain.Services;
using Serilog;

namespace Robalo.Domain.UnitTests.Services;

public class AnswerThreadServiceTests : IDisposable
{
    readonly AnswerThreadService _sut;
    readonly Mock<ILlmClient> _llmClient = new();
    readonly Mock<IThreadRepository> _repository = new();
    readonly Mock<IDateTimeOffsetProvider> _datetimeOffsetProvider = new();
    readonly Fixture _fixture = new();
    readonly DateTimeOffset _utcNow;
    LlmResponse _llmResponse;

    readonly int _messageNumber;

    public AnswerThreadServiceTests()
    {
        _sut = new AnswerThreadService(Log.Logger, _llmClient.Object, _repository.Object, _datetimeOffsetProvider.Object);
        _utcNow = _fixture.Create<DateTimeOffset>();
        _datetimeOffsetProvider.SetupGet(d => d.UtcNow).Returns(_utcNow);
        _llmResponse = _fixture.Create<LlmResponse>();
        _llmClient
            .Setup(l => l.GetResponse(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_llmResponse);

        _messageNumber = _fixture.Create<int>();
        _repository
            .Setup(r => r.AppendEvent(It.IsAny<ThreadIdentifier>(), It.IsAny<LlmMessageAdded>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_messageNumber);
    }

    [Fact]
    public async Task Answer_ShouldReturnSuccess_OnNonExistingThread()
    {
        var threadId = ThreadIdentifier.NewIdentifier();
        _repository
            .Setup(r => r.GetThread(threadId, TestContext.Current.CancellationToken))
            .ReturnsAsync(None.Default);

        var result = await _sut.Answer(threadId, TestContext.Current.CancellationToken);
        result.Value.ShouldBeOfType<Success>();
    }

    [Fact]
    public async Task Answer_ShouldCorrectlyProcess()
    {
        var thread = Thread.NewThread(_utcNow.AddSeconds(-4));
        thread.AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(-3))
            .AddLlmMessage(_fixture.Create<string>(), thread.Messages[^1], _utcNow.AddSeconds(-2))
            .AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(-1));

        _repository
            .Setup(r => r.GetThread(thread.Id, TestContext.Current.CancellationToken))
            .ReturnsAsync(thread);

        var result = await _sut.Answer(thread.Id, TestContext.Current.CancellationToken);

        result.Value.ShouldBeOfType<Success>();

        _llmClient.Verify(
            l => l.GetResponse(It.Is<LlmRequest>(
                r => r.Messages.Length == 3 &&
                r.Messages[0].Content == thread.Messages[0].Content &&
                r.Messages[0].Source == MessageSource.User &&
                r.Messages[1].Content == thread.Messages[1].Content &&
                r.Messages[1].Source == MessageSource.LLM &&
                r.Messages[2].Content == thread.Messages[2].Content &&
                r.Messages[2].Source == MessageSource.User),
            TestContext.Current.CancellationToken), Times.Once());
        _llmClient.VerifyNoOtherCalls();

        _repository.Verify(r => r.AppendEvent(thread.Id, It.Is<LlmMessageAdded>(
            l => l.AddedOn == _utcNow
            && l.Content == _llmResponse.Content
            && l.RepliedToMessageId == thread.Messages[2].MessageId
            && l.MessageId != l.RepliedToMessageId),
            TestContext.Current.CancellationToken), Times.Once());

        _repository.Verify(r => r.RefreshThread(thread, TestContext.Current.CancellationToken), Times.Once());
    }

    [Fact]
    public async Task Answer_ShouldReturnSuccess_IfThreadGotDeletedAfterLlmRequest()
    {
        var thread = Thread.NewThread(_utcNow.AddSeconds(-4))
            .AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(-2))
            .AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(-1));

        _repository
            .Setup(r => r.GetThread(thread.Id, TestContext.Current.CancellationToken))
            .ReturnsAsync(thread);

        _repository
            .Setup(r => r.AppendEvent(It.IsAny<ThreadIdentifier>(), It.IsAny<LlmMessageAdded>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(None.Default);

        var result = await _sut.Answer(thread.Id, TestContext.Current.CancellationToken);

        result.Value.ShouldBeOfType<Success>();

        _llmClient.Verify(
            l => l.GetResponse(It.Is<LlmRequest>(r => r.Messages.Length == 2),
            TestContext.Current.CancellationToken), Times.Once());
        _llmClient.VerifyNoOtherCalls();

        _repository.Verify(r => r.AppendEvent(thread.Id, It.Is<LlmMessageAdded>(
            l => l.AddedOn == _utcNow
            && l.Content == _llmResponse.Content
            && l.RepliedToMessageId == thread.Messages[1].MessageId
            && l.MessageId != l.RepliedToMessageId),
            TestContext.Current.CancellationToken), Times.Once());
    }

    [Fact]
    public async Task Answer_ShouldReturnSuccess_IfThreadGotDeletedAfterRefresh()
    {
        var thread = Thread.NewThread(_utcNow.AddSeconds(-4))
            .AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(-2))
            .AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(-1));

        _repository
            .Setup(r => r.GetThread(thread.Id, TestContext.Current.CancellationToken))
            .ReturnsAsync(thread);

        _repository.Setup(r => r.RefreshThread(thread, TestContext.Current.CancellationToken))
            .ReturnsAsync(new NotFound());


        var result = await _sut.Answer(thread.Id, TestContext.Current.CancellationToken);

        result.Value.ShouldBeOfType<Success>();

        _llmClient.VerifyNoOtherCalls();

        _repository.Verify(r => r.GetThread(thread.Id, TestContext.Current.CancellationToken), Times.Once());
        _repository.Verify(r => r.RefreshThread(thread, TestContext.Current.CancellationToken), Times.Once());
        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Answer_ShouldReturnSuccess_IfThreadAlreadyAnswered()
    {
        var thread = Thread.NewThread(_utcNow.AddSeconds(-4))
            .AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(-2));

        thread.AddLlmMessage(_fixture.Create<string>(), thread.Messages.Single(), _utcNow);
        thread.IsAnswered.ShouldBeTrue();

        _repository
            .Setup(r => r.GetThread(thread.Id, TestContext.Current.CancellationToken))
            .ReturnsAsync(thread);

        var result = await _sut.Answer(thread.Id, TestContext.Current.CancellationToken);

        result.Value.ShouldBeOfType<Success>();

        _llmClient.VerifyNoOtherCalls();

        _repository.Verify(r => r.GetThread(thread.Id, TestContext.Current.CancellationToken), Times.Once());
        _repository.Verify(r => r.RefreshThread(thread, TestContext.Current.CancellationToken), Times.Once());
        _repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Answer_ShouldReturnSuccess_IfThreadAlreadyAnswered_AtRefresh()
    {
        var thread = Thread.NewThread(_utcNow.AddSeconds(-4))
            .AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(-2));

        thread.IsAnswered.ShouldBeFalse();

        _repository
            .Setup(r => r.GetThread(thread.Id, TestContext.Current.CancellationToken))
            .ReturnsAsync(thread);

        _repository
            .Setup(r => r.RefreshThread(thread, TestContext.Current.CancellationToken))
            .ReturnsAsync(new Success())
            .Callback(() => thread.AddLlmMessage(_fixture.Create<string>(), thread.Messages.Single(), _utcNow));

        var result = await _sut.Answer(thread.Id, TestContext.Current.CancellationToken);

        result.Value.ShouldBeOfType<Success>();

        _llmClient.VerifyNoOtherCalls();

        _repository.Verify(r => r.GetThread(thread.Id, TestContext.Current.CancellationToken), Times.Once());
        _repository.Verify(r => r.RefreshThread(thread, TestContext.Current.CancellationToken), Times.Once());
        _repository.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(typeof(ClientError))]
    [InlineData(typeof(UnexpectedError))]
    [InlineData(typeof(ServiceError))]
    public async Task Answer_ShouldReturnFailure_OnLlmFailure(Type llmFailure)
    {
        var thread = Thread.NewThread(_utcNow.AddSeconds(-4))
            .AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(-2))
            .AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(-1));

        _repository
            .Setup(r => r.GetThread(thread.Id, TestContext.Current.CancellationToken))
            .ReturnsAsync(thread);

        _repository
            .Setup(r => r.AppendEvent(It.IsAny<ThreadIdentifier>(), It.IsAny<LlmMessageAdded>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(None.Default);


        ServiceResult<LlmResponse, ClientError> llmServiceError = new ServiceError();
        if (llmFailure == typeof(UnexpectedError))
        {
            llmServiceError = new UnexpectedError();
        }
        if (llmFailure == typeof(ClientError))
        {
            llmServiceError = ClientError.AsForbidden();
        }

        _llmClient
            .Setup(r => r.GetResponse(It.IsAny<LlmRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmServiceError);

        var result = await _sut.Answer(thread.Id, TestContext.Current.CancellationToken);

        result.Value.ShouldBeOfType<Failure>();

        _llmClient.Verify(
            l => l.GetResponse(It.Is<LlmRequest>(r => r.Messages.Length == 2),
            TestContext.Current.CancellationToken), Times.Once());
        _llmClient.VerifyNoOtherCalls();

        _repository.Verify(r => r.GetThread(thread.Id, TestContext.Current.CancellationToken), Times.Once());
        _repository.Verify(r => r.RefreshThread(thread, TestContext.Current.CancellationToken), Times.Once());

        _repository.VerifyNoOtherCalls();
    }

    public void Dispose()
    {
        _sut.Dispose();
        GC.SuppressFinalize(this);
    }
}