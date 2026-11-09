using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Robalo.Common.Models;

namespace Robalo.Domain.Configuration;

public sealed record RobaloConfiguration(
    int ConfigurationId,
    DateTimeOffset CreatedOn,
    RobaloOptions Value);

public sealed record RobaloOptions([property: JsonPropertyName("Llm")] LlmOptions? LlmOptions)
{
    [MemberNotNull(nameof(LlmOptions))]
    public void Verify()
    {
        if (LlmOptions is null)
        {
            throw new InvalidOperationException($"{nameof(LlmOptions)} must be provided");
        }
        LlmOptions.Verify();
    }

    public RobaloOptions ToSafeOptions() => new RobaloOptions(LlmOptions?.ToSafeOptions());
}