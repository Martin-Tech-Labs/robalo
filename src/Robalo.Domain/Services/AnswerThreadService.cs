using System.Collections.Concurrent;
using System.Formats.Asn1;
using System.Runtime.CompilerServices;
using Robalo.Common.Extensions;
using Robalo.Common.Models;
using Robalo.Common.Semaphore;
using Robalo.Common.Services;
using Robalo.Domain.Clients;
using Robalo.Domain.Clients.Models;
using Robalo.Domain.Models;
using Robalo.Domain.Repositories;
using Serilog;

namespace Robalo.Domain.Services;

public sealed class AnswerThreadService : IAnswerThreadService, IDisposable
{
    readonly ILogger _logger;
    readonly ILlmClient _llmClient;
    readonly IThreadRepository _repository;
    readonly IDateTimeOffsetProvider _dateTimeOffSetProvider;

    readonly Lock _lockSemaphors = new();
    readonly SemaphoreWrapper _semaphoreThreads = new SemaphoreWrapper(new SemaphoreSlim(1, 1));
    readonly Dictionary<ThreadIdentifier, SemaphoreWrapper> _semaphors = new();
    readonly ConcurrentDictionary<ThreadIdentifier, Thread> _threads = new();

    public AnswerThreadService(
        ILogger logger,
        ILlmClient llmClient,
        IThreadRepository threadRepository,
        IDateTimeOffsetProvider dateTimeOffsetProvider)
    {
        _logger = logger.ForContext(GetType());
        _llmClient = llmClient;
        _repository = threadRepository;
        _dateTimeOffSetProvider = dateTimeOffsetProvider;
    }

    async Task<Option<Thread>> GetThread(ThreadIdentifier threadId, CancellationToken cancellationToken)
    {
        if (_threads.TryGetValue(threadId, out var cachedThread))
        {
            return cachedThread;
        }

        using var semaphore = await _semaphoreThreads.Wait(cancellationToken);

        if (_threads.TryGetValue(threadId, out var cachedThread2))
        {
            return cachedThread2;
        }

        var threadOrNone = await _repository.GetThread(threadId, cancellationToken);

        if (threadOrNone is Some<Thread> thread)
        {
            _threads.TryAdd(threadId, thread);
            return _threads[threadId];
        }

        return None.Default;
    }

    private Task<IDisposable> AcquirerSemaphore(ThreadIdentifier threadId, CancellationToken cancellationToken)
    {
        lock (_lockSemaphors)
        {
            if (_semaphors.TryGetValue(threadId, out var semaphoreSlim))
            {
                return semaphoreSlim.Wait(cancellationToken);
            }

            var semaphoreWrapper = new SemaphoreWrapper(new SemaphoreSlim(1, 1));
            _semaphors[threadId] = semaphoreWrapper;
            return semaphoreWrapper.Wait(cancellationToken);
        }
    }

    public async Task Answer(ThreadIdentifier threadId, CancellationToken cancellationToken)
    {
        var threadOrNone = await GetThread(threadId, cancellationToken);

        if (!threadOrNone.HasValue)
        {
            _logger.Warning("Thread {ThreadId} does not exist", threadId);
            return;
        }

        using var semaphore = await AcquirerSemaphore(threadId, cancellationToken);

        var thread = threadOrNone.ValueOrFailure;

        var refreshResult = await _repository.RefreshThread(thread, cancellationToken);

        if (refreshResult is NotFound)
        {
            _threads.Remove(thread.Id, out _);

            lock (_lockSemaphors)
            {
                _semaphors.Remove(threadId);
            }

            return;
        }

        if (thread.IsAnswered)
        {
            _logger.Information("Thread {ThreadId} is already answered", threadId);
            return;
        }

        var llmRequest = new LlmRequest(string.Concat(thread.Messages));

        var llmResponse = await _llmClient.GetResponse(llmRequest, cancellationToken);

        if (llmResponse is LlmResponse response)
        {
            var llmMessageAdded = thread.CreateLlmMessageAdded(response.Content, thread.Messages[^1], _dateTimeOffSetProvider.UtcNow);
            var result = await _repository.AppendEvent(threadId, llmMessageAdded, cancellationToken);

            if (result is Some<int> messageNumber)
            {
                _logger.Information("LLM Message successfully added under number {MessageNumber} for Thread {ThreadId}", messageNumber.Value, threadId);
                return;
            }

            _threads.Remove(threadId, out _);

            lock (_lockSemaphors)
            {
                _semaphors.Remove(threadId);
            }

            _logger.Warning("Thread {ThreadId} was deleted", threadId);
            return;

        }
    }

    public void Dispose()
    {
        lock (_lockSemaphors)
        {
            _semaphoreThreads.Dispose();
            foreach (var semaphore in _semaphors.Values)
            {
                semaphore.Dispose();
            }
        }
    }
}