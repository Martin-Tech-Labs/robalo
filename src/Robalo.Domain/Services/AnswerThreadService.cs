using System.Collections.Concurrent;
using System.Formats.Asn1;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using OpenAI.Responses;
using Robalo.Common.Extensions;
using Robalo.Common.Models;
using Robalo.Common.Rop;
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
            _logger.Debug("Retrieved thread {ThreadId} from internal cache", threadId);
            return cachedThread;
        }

        using var semaphore = await _semaphoreThreads.Wait(cancellationToken);
        _logger.Debug("Acquiring global semaphore while trying to get thread {ThreadId}", threadId);


        if (_threads.TryGetValue(threadId, out var cachedThread2))
        {
            _logger.Debug("Retrieved thread {ThreadId} from internal cache after semaphore wait.", threadId);
            return cachedThread2;
        }

        var threadOrNone = await _repository.GetThread(threadId, cancellationToken);

        if (threadOrNone is Some<Thread> thread)
        {
            _logger.Debug("Retrieved thread {ThreadId} from Repository.", threadId);

            _threads.TryAdd(threadId, thread);
            return _threads[threadId];
        }

        _logger.Debug("Thread {ThreadId} does not exist.", threadId);


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

    public async Task<SuccessOrFailure> Answer(ThreadIdentifier threadId, CancellationToken cancellationToken)
    {
        _logger.Debug("Starting Answer for Thread {ThreadId}", threadId);
        var threadOrNone = await GetThread(threadId, cancellationToken);

        if (!threadOrNone.HasValue)
        {
            _logger.Warning("Thread {ThreadId} does not exist", threadId);
            return SuccessOrFailure.AsSuccess();
        }

        _logger.Debug("Acquiring the semaphore for thread {ThreadId}.", threadId);
        using var semaphore = await AcquirerSemaphore(threadId, cancellationToken);
        _logger.Debug("Acquired the semaphore for thread {ThreadId}.", threadId);


        var thread = threadOrNone.ValueOrFailure;

        return await RefreshThread(thread)
            .Bind(CheckIsAnswered)
            .Bind(GetLlmResponse)
            .Map(CheckLlmResponse)
            .Either();

        async Task<Result<Thread, SuccessOrFailure>> RefreshThread(Thread thread)
        {
            _logger.Debug("Refreshing thread {ThreadId} from the repository.", threadId);

            var versionBefore = thread.PersistedVersion.ValueOrFailure;

            var refreshResult = await _repository.RefreshThread(thread, cancellationToken);

            if (refreshResult is NotFound)
            {
                _logger.Debug("Refreshing thread {ThreadId} returned {Result}.", threadId, refreshResult.Value.GetType().Name);

                _threads.Remove(thread.Id, out _);

                lock (_lockSemaphors)
                {
                    _semaphors.Remove(threadId);
                }

                return SuccessOrFailure.AsSuccess();
            }

            _logger.Debug("Refreshed thread {ThreadId}: Version {OldVersion} -> {NewVersion}", threadId, versionBefore, thread.PersistedVersion.ValueOrFailure);

            return thread;
        }

        Result<Thread, SuccessOrFailure> CheckIsAnswered(Thread thread)
        {
            if (thread.IsAnswered)
            {
                _logger.Information("Thread {ThreadId} is already answered", threadId);
                return SuccessOrFailure.AsSuccess();
            }

            return thread;
        }

        async Task<Result<LlmResponse, SuccessOrFailure>> GetLlmResponse(Thread thread)
        {
            var llmRequest = new LlmRequest([.. thread.Messages.Select(ThreadMessage.FromMessage)]);

            var llmResponse = await _llmClient.GetResponse(llmRequest, cancellationToken);

            if (llmResponse is LlmResponse response)
            {
                return response;
            }

            _logger.Error("LLM returned an error respone {Error}", llmResponse.Value);
            return SuccessOrFailure.AsFailure();
        }

        async Task<SuccessOrFailure> CheckLlmResponse(LlmResponse response)
        {
            var llmMessageAdded = thread.CreateLlmMessageAdded(response.Content, thread.Messages[^1], _dateTimeOffSetProvider.UtcNow);
            var result = await _repository.AppendEvent(threadId, llmMessageAdded, cancellationToken);

            if (result is Some<int> messageNumber)
            {
                _logger.Information("LLM Message successfully added under number {MessageNumber} for Thread {ThreadId}", messageNumber.Value, threadId);
                return SuccessOrFailure.AsSuccess();
            }

            _threads.Remove(threadId, out _);

            lock (_lockSemaphors)
            {
                _semaphors.Remove(threadId);
            }

            _logger.Warning("Thread {ThreadId} was deleted", threadId);
            return SuccessOrFailure.AsSuccess();
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