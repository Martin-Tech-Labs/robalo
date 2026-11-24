using System.ClientModel;
using Moq;
using OpenAI.Chat;
using Robalo.Common.Configuration;
using Robalo.Common.Models;
using Robalo.Domain.Clients;
using Robalo.Domain.Clients.Models;
using Robalo.Domain.Models;
using Serilog;

namespace Robalo.Domain.IntegrationTests.Clients;

public class LlmClientTests
{
    [Fact]
    public async Task ShouldReturn_Success()
    {
        var endpoint = new Uri("http://localhost:11500/v1/");
        var model = "test-model";
        var client = new ChatClient(
          model: model,
          credential: new ApiKeyCredential("test"),
          options: new OpenAI.OpenAIClientOptions
          {
              Endpoint = endpoint,
          });

        var smart = new Mock<ISmart<ChatClient>>();
        smart.SetupGet(s => s.Value).Returns(client);

        var sut = new LlmClient(Log.Logger, smart.Object);

        var result = await sut.GetResponse(new LlmRequest(
            new ThreadMessage("Hi there", MessageRole.User),
            new ThreadMessage("Hi!", MessageRole.Assistant),
            new ThreadMessage("How are you?", MessageRole.User)), TestContext.Current.CancellationToken);
        result.Value.ShouldBeOfType<LlmResponse>();

        var llmResponse = (LlmResponse)result.Value;
        llmResponse.Metadata.Endpoint.ShouldBe(endpoint);
        llmResponse.Metadata.Model.ShouldBe(model);
        llmResponse.Metadata.InputTokens.HasValue.ShouldBeTrue();
        llmResponse.Metadata.OutputTokens.HasValue.ShouldBeTrue();
        llmResponse.Metadata.InputTokens.Value.ShouldBeGreaterThan(0);
        llmResponse.Metadata.OutputTokens.Value.ShouldBeGreaterThan(0);
        llmResponse.Content.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task ShouldReturn_ClientError_OnNotFound()
    {
        var client = new ChatClient(
          model: "test-model",
          credential: new ApiKeyCredential("test"),
          options: new OpenAI.OpenAIClientOptions
          {
              Endpoint = new Uri("http://localhost:11500/vd1/"),
          });

        var smart = new Mock<ISmart<ChatClient>>();
        smart.SetupGet(s => s.Value).Returns(client);

        var sut = new LlmClient(Log.Logger, smart.Object);

        var result = await sut.GetResponse(new LlmRequest(new ThreadMessage("Hi there", MessageRole.User)), TestContext.Current.CancellationToken);
        result.Value.ShouldBeOfType<ClientError>();

        var clientError = (ClientError)result.Value;
        clientError.ErrorType.ShouldBe(ClientError.ClientErrorType.NotFound);
    }

    [Fact]
    public async Task ShouldThrow_On_CancellationToken()
    {
        var client = new ChatClient(
          model: "test-model",
          credential: new ApiKeyCredential("test"),
          options: new OpenAI.OpenAIClientOptions
          {
              Endpoint = new Uri("http://localhost:11500/v1/"),
          });

        var smart = new Mock<ISmart<ChatClient>>();
        smart.SetupGet(s => s.Value).Returns(client);

        var sut = new LlmClient(Log.Logger, smart.Object);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() => sut.GetResponse(new LlmRequest(new ThreadMessage("Hi there", MessageRole.User)), cts.Token));
    }
}