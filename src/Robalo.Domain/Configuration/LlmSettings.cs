using Robalo.Common.Models;

namespace Robalo.Domain.Configuration;
public record LlmSettings(Uri Endpoint, string Model, string Key);