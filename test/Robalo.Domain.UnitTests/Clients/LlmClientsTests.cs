using System.ClientModel;
using System.ClientModel.Primitives;
using System.Diagnostics;
using Moq;
using OpenAI.Chat;
using Robalo.Common.Models;
using Robalo.Domain.Clients;
using Robalo.Domain.Clients.Models;
using Robalo.Domain.Models;
using Serilog;
using static Robalo.Common.Models.ClientError;

namespace Robalo.Domain.UnitTests.Services;

public class LlmClientsTests
{
    readonly LlmClient _sut;
    readonly Mock<ChatClient> _chatClient = new();
    readonly Fixture _fixture = new();

    public LlmClientsTests()
    {
        _sut = new LlmClient(Log.Logger, _chatClient.Object);
    }

    [Fact]
    public async Task ShouldReturn_Response_WithSinglePart()
    {
        var expectedResponse = _fixture.Create<string>();
        var llmRequest = new LlmRequest(
            new ThreadMessage(_fixture.Create<string>(), MessageSource.User),
            new ThreadMessage(_fixture.Create<string>(), MessageSource.LLM),
            new ThreadMessage(_fixture.Create<string>(), MessageSource.User));

        ConfigureResponse(expectedResponse);

        var result = await _sut.GetResponse(llmRequest, TestContext.Current.CancellationToken);

        _chatClient.Verify(c => c.CompleteChatAsync(
            It.Is<IEnumerable<ChatMessage>>(messages =>
                messages.Count() == 3 &&
                messages.First() is UserChatMessage &&
                messages.First().Content.Single().Text == llmRequest.Messages[0].Content &&
                messages.ElementAt(1) is AssistantChatMessage &&
                messages.ElementAt(1).Content.Single().Text == llmRequest.Messages[1].Content &&
                messages.ElementAt(2) is UserChatMessage &&
                messages.ElementAt(2).Content.Single().Text == llmRequest.Messages[2].Content),
            It.IsAny<ChatCompletionOptions>(),
            TestContext.Current.CancellationToken),
            Times.Once);

        result.Value.ShouldBeOfType<LlmResponse>();
        var llmResponse = (LlmResponse)result.Value;
        llmResponse.Content.ShouldBe(expectedResponse);
    }

    [Fact]
    public async Task ShouldReturn_Response_WithMultipleParts()
    {
        var expectedResponses = _fixture.CreateMany<string>(_fixture.Create<int>());
        ConfigureResponse(expectedResponses);

        var result = await _sut.GetResponse(_fixture.Create<LlmRequest>(), TestContext.Current.CancellationToken);

        result.Value.ShouldBeOfType<LlmResponse>();
        var llmResponse = (LlmResponse)result.Value;
        llmResponse.Content.ShouldBe(string.Concat(expectedResponses));
    }

    [Theory]
    [InlineData(401, ClientErrorType.Unauthorized)]
    [InlineData(403, ClientErrorType.Forbidden)]
    [InlineData(404, ClientErrorType.NotFound)]
    public async Task ShouldReturn_ClientError_On_401_or_403_or_404(int status, ClientErrorType errorType)
    {
        var response = Mock.Of<PipelineResponse>((r => r.Status == status));
        var exception = new ClientResultException(_fixture.Create<string>(), response);

        _chatClient
             .Setup(c => c.CompleteChatAsync(
                 It.IsAny<IEnumerable<ChatMessage>>(),
                 It.IsAny<ChatCompletionOptions>(),
                 TestContext.Current.CancellationToken))
             .ThrowsAsync(exception);

        var result = await _sut.GetResponse(_fixture.Create<LlmRequest>(), TestContext.Current.CancellationToken);
        result.Value.ShouldBeOfType<ClientError>();
        var authError = (ClientError)result.Value;
        authError.ErrorType.ShouldBe(errorType);
    }

    [Theory]
    [InlineData(500)]
    [InlineData(422)]
    public async Task ShouldReturn_ServiceError(int status)
    {
        var response = Mock.Of<PipelineResponse>((r => r.Status == status));
        var exception = new ClientResultException("error", response);

        _chatClient
             .Setup(c => c.CompleteChatAsync(
                 It.IsAny<IEnumerable<ChatMessage>>(),
                 It.IsAny<ChatCompletionOptions>(),
                 TestContext.Current.CancellationToken))
             .ThrowsAsync(exception);

        var result = await _sut.GetResponse(_fixture.Create<LlmRequest>(), TestContext.Current.CancellationToken);
        result.Value.ShouldBeOfType<ServiceError>();
    }

    [Fact]
    public async Task ShouldReturn_UnexpectedError()
    {
        var response = Mock.Of<PipelineResponse>();
        var exception = new ArgumentOutOfRangeException();

        _chatClient
             .Setup(c => c.CompleteChatAsync(
                 It.IsAny<IEnumerable<ChatMessage>>(),
                 It.IsAny<ChatCompletionOptions>(),
                 TestContext.Current.CancellationToken))
             .ThrowsAsync(exception);

        var result = await _sut.GetResponse(_fixture.Create<LlmRequest>(), TestContext.Current.CancellationToken);
        result.Value.ShouldBeOfType<UnexpectedError>();
    }

    [Fact]
    public async Task ShouldThrow_If_NoMessages()
    {
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() => _sut.GetResponse(new LlmRequest(), TestContext.Current.CancellationToken));
    }

    private void ConfigureResponse(string expectedResponse)
    {
#pragma warning disable OPENAI001
        var completion = OpenAIChatModelFactory.ChatCompletion(content: new ChatMessageContent(expectedResponse));
#pragma warning restore

        var chatClientResponse = ClientResult.FromValue(completion, Mock.Of<PipelineResponse>());

        _chatClient
            .Setup(c => c.CompleteChatAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatCompletionOptions>(),
                TestContext.Current.CancellationToken))
            .ReturnsAsync(chatClientResponse);
    }

    private void ConfigureResponse(params IEnumerable<string> expectedResponses)
    {
#pragma warning disable OPENAI001
        var completion = OpenAIChatModelFactory.ChatCompletion(content: new ChatMessageContent(
            expectedResponses.Select(e => ChatMessageContentPart.CreateTextPart(e))));
#pragma warning restore

        var chatClientResponse = ClientResult.FromValue(completion, Mock.Of<PipelineResponse>());

        _chatClient
            .Setup(c => c.CompleteChatAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatCompletionOptions>(),
                TestContext.Current.CancellationToken))
            .ReturnsAsync(chatClientResponse);
    }
}