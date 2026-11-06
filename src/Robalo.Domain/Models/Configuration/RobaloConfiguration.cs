using System.Diagnostics.CodeAnalysis;
using Robalo.Common.Models;

namespace Robalo.Domain.Models.Configuration;

public sealed record RobaloConfiguration(
    int ConfigurationId,
    DateTimeOffset CreatedOn,
    RobaloOptions Value);

public sealed record RobaloOptions(LlmOptions? LlmOptions)
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
}