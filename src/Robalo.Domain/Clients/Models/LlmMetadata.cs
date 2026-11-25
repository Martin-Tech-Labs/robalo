using Robalo.Common.Models;

namespace Robalo.Domain.Clients.Models;

public record LlmMetadata(Uri Endpoint, string Model, Option<int> InputTokens, Option<int> OutputTokens);