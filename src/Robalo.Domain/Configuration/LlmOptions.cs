using System.Diagnostics.CodeAnalysis;
using Robalo.Common.Extensions;

namespace Robalo.Domain.Configuration;

public sealed class LlmOptions
{
    public Uri? Endpoint { get; set; }
    public string? Model { get; set; }
    public string? Key { get; set; }

    public LlmSettings AsSettings()
    {
        Verify();
        return new LlmSettings(Endpoint!, Model, Key);
    }

    [MemberNotNull(nameof(Model))]
    [MemberNotNull(nameof(Key))]
    [MemberNotNull(nameof(Endpoint))]
    public void Verify()
    {
        if (Endpoint is null || !Endpoint.IsAbsoluteUri)
        {
            throw new InvalidOperationException($"{nameof(Endpoint)} must be provided and be an absolute URI");
        }

        if (string.IsNullOrWhiteSpace(Model))
        {
            throw new InvalidOperationException($"{nameof(Model)} must be provided");
        }

        if (string.IsNullOrWhiteSpace(Key))
        {
            throw new InvalidOperationException($"{nameof(Key)} must be provided");
        }
    }
}