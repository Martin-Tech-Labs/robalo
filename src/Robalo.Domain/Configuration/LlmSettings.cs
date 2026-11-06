using Robalo.Common.Models;

namespace Robalo.Domain.Configuration;
public record LlmSettings(Option<Uri> Endpoint, string Model, string Key);