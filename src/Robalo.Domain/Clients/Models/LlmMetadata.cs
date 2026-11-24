namespace Robalo.Domain.Clients.Models;

public record LlmMetadata(Uri Endpoint, string Model, int? InputTokens, int? OutputTokens);