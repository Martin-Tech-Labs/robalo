using Robalo.Common.Models;

namespace Robalo.Domain.Services.Configuration;

public record LlmSettings(Option<Uri> Endpoint, string Model, string Key);