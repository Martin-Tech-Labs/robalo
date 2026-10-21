using System.ClientModel;
using OpenAI.Chat;
using Robalo.Common.Models;
using Robalo.Domain.Clients;
using Robalo.Domain.Clients.Models;
using Serilog;

namespace Robalo.Domain.IntegrationTests.Clients;

public class LlmClientTests
{
    [Fact]
    public async Task ShouldReturn_Success()
    {
        var client = new ChatClient(
          model: "test-model",
          credential: new ApiKeyCredential("test"),
          options: new OpenAI.OpenAIClientOptions
          {
              Endpoint = new Uri("http://localhost:11500/v1/"),
          });

        var sut = new LlmClient(Log.Logger, client);

        var result = await sut.GetResponse(new LlmRequest("Hi there"), TestContext.Current.CancellationToken);
        result.Value.ShouldBeOfType<LlmResponse>();

        var llmResponse = (LlmResponse)result.Value;
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

        var sut = new LlmClient(Log.Logger, client);

        var result = await sut.GetResponse(new LlmRequest("Hi there"), TestContext.Current.CancellationToken);
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

        var sut = new LlmClient(Log.Logger, client);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() => sut.GetResponse(new LlmRequest("Hi there"), cts.Token));
    }
}