using System.Diagnostics.CodeAnalysis;
using Robalo.Common.Extensions;

namespace Robalo.Domain.Clients.Configuration;

public sealed class LlmOptions
{
    public Uri? Endpoint { get; set; }
    public string? Model { get; set; }
    public string? Key { get; set; }

    public LlmSettings AsSettings()
    {
        Verify();
        return new LlmSettings(Endpoint.AsOption(), Model, Key);
    }

    [MemberNotNull(nameof(Model))]
    [MemberNotNull(nameof(Key))]
    public void Verify()
    {
        if (Endpoint is not null && !Endpoint.IsAbsoluteUri)
        {
            throw new InvalidOperationException($"If provided, {nameof(Endpoint)} must be an absolute URI");
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