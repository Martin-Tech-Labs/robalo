namespace Robalo.Domain.Clients.Models;

public sealed record LlmRequest(params ThreadMessage[] Messages);