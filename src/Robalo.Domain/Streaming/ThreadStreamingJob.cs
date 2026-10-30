using Hangfire.Logging;
using Robalo.Common.Streaming;
using Robalo.Domain.Models;
using Serilog;

namespace Robalo.Domain.Streaming;

public sealed class StreamThreadJob
{
    private readonly ILogger _logger;
    private readonly IStreamingService<ThreadIdentifier, Message, int> _streamingService;

    public StreamThreadJob(
        ILogger logger,
        IStreamingService<ThreadIdentifier, Message, int> streamingService)
    {
        _logger = logger.ForContext(GetType());
        _streamingService = streamingService;
    }

    public async Task Stream(string threadId, CancellationToken cancellationToken) =>await _streamingService.PushForStream(ThreadIdentifier.FromIdString(threadId), cancellationToken);
}