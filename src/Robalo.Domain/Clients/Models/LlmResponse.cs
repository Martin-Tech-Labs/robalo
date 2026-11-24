namespace Robalo.Domain.Clients.Models;

public sealed record LlmResponse(string Content, LlmMetadata Metadata);