using System.ClientModel;
using OpenAI;
using OpenAI.Chat;
using Robalo.Common.Extensions;
using Robalo.Common.Models;
using Robalo.Domain.Clients.Configuration;
using Robalo.Domain.Clients.Models;
using Serilog;

namespace Robalo.Domain.Clients;

public sealed class LlmClient : ILlmClient
{
    readonly ILogger _logger;
    readonly ChatClient _chatClient;

    public LlmClient(ILogger logger, ChatClient chatClient)
    {
        _logger = logger.ForContext(GetType());
        _chatClient = chatClient;
    }

    public async Task<ServiceResult<LlmResponse, ClientError>> GetResponse(LlmRequest request, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Content);

        try
        {
            var response = await _chatClient.CompleteChatAsync(
                messages: new[] { ChatMessage.CreateUserMessage(request.Content) },
                options: null,
                cancellationToken: cancellationToken);

            var text = string.Concat(response.Value.Content.Select(c => c.Text));
            return new LlmResponse(text);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw; // Preserve caller cancellation.
        }
        catch (ClientResultException ex) when (ex.Status is 401)
        {
            _logger.Error(ex, "LLM request failed with Unauthorized error with status {Status}", ex.Status);
            return ClientError.AsUnauthorized();
        }
        catch (ClientResultException ex) when (ex.Status is 403)
        {
            _logger.Error(ex, "LLM request failed with Forbidden error with status {Status}", ex.Status);
            return ClientError.AsForbidden();
        }
        catch (ClientResultException ex) when (ex.Status is 404)
        {
            _logger.Error(ex, "LLM request failed with NotFound error with status {Status}", ex.Status);
            return ClientError.AsNotFound();
        }
        catch (ClientResultException ex)
        {
            _logger.Error(ex, "LLM request failed with status {Status}", ex.Status);
            return new ServiceError();
        }
#pragma warning disable CC019
        catch (Exception ex)
        {
            _logger.Error(ex, "Unexpected failure calling the LLM");
            return new UnexpectedError();
        }
#pragma warning restore
    }
}