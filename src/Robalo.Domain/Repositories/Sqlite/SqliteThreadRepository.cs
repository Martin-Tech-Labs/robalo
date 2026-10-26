using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Transactions;
using Dapper;
using Microsoft.Data.Sqlite;
using OpenAI.Assistants;
using Robalo.Common.Extensions;
using Robalo.Common.Models;
using Robalo.Domain.Events;
using Robalo.Domain.Models;
using Robalo.Domain.Repositories.Sqlite.Dto;
using Serilog;
using SQLitePCL;

namespace Robalo.Domain.Repositories.Sqlite;

public sealed class SqliteThreadRepository : IThreadRepository
{
    readonly ILogger _logger;
    readonly ISqliteConnectionFactory _sqlConnectionFactory;

    public SqliteThreadRepository(ILogger logger, ISqliteConnectionFactory sqlConnectionFactory)
    {
        _logger = logger.ForContext(GetType());
        _sqlConnectionFactory = sqlConnectionFactory;
    }
    public async Task<SuccessOrNotFound> DeleteThread(ThreadIdentifier identifier, CancellationToken cancellationToken) =>
        await _sqlConnectionFactory.ExecuteAsTransaction<SuccessOrNotFound>(async connection =>
         {
             var rowsAffected = await connection.ExecuteAsync("DELETE FROM Threads WHERE Id = @ThreadId", new { ThreadId = identifier });

             if (rowsAffected == 0)
             {
                 return new NotFound();
             }

             if (rowsAffected != 1)
             {
                 throw new InvalidOperationException($"Returned rows affected {rowsAffected}, but expected 0 or 1");
             }

             rowsAffected = await connection.ExecuteAsync("DELETE FROM ThreadEvents WHERE ThreadId = @ThreadId", new { ThreadId = identifier });

             return new Success();

         }, cancellationToken);

    public async Task<Option<Message>> GetMessage(ThreadIdentifier identifier, int messageNumber, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(messageNumber, 1);

        return await _sqlConnectionFactory.Execute(async connection =>
        {
            return (await connection.QueryAsync<NumberedEventDto>(
            """
            SELECT *
            FROM ThreadEvents
            WHERE ThreadId = @ThreadId AND MessageNumber = @MessageNumber
            """,
                new
                {
                    ThreadId = identifier,
                    MessageNumber = messageNumber
                }))
                .Select(dto => (Event: EventDto.ToModel(dto), dto.MessageNumber))
                .SingleOrNone(row => Message.FromModel(row.Event, row.MessageNumber));
        }, cancellationToken);
    }

    public async Task<Option<IAsyncEnumerable<Message>>> GetMessages(ThreadIdentifier identifier, CancellationToken cancellationToken)
    {
        return await ThreadExists(identifier, cancellationToken) switch
        {
            false => None.Default,
            true => GetEnumerable(cancellationToken).Some()
        };

        async IAsyncEnumerable<Message> GetEnumerable([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            Option<Message> messageOrNone = None.Default;
            var messageNumber = 1;
            do
            {
                cancellationToken.ThrowIfCancellationRequested();
                messageOrNone = await GetMessage(identifier, messageNumber++, cancellationToken);
                if (messageOrNone.HasValue)
                {
                    yield return messageOrNone.ValueOrFailure;
                }
            }
            while (messageOrNone.HasValue);
        }
    }

    private async Task<bool> ThreadExists(ThreadIdentifier identifier, CancellationToken cancellationToken)
    {
        return await _sqlConnectionFactory.Execute(async connection =>
        {
            return (await connection.QueryAsync<ThreadDto>("SELECT * FROM Threads WHERE Id = @Id", new { Id = identifier })).Any();

        }, cancellationToken);
    }

    public async Task<Option<Thread>> GetThread(ThreadIdentifier identifier, CancellationToken cancellationToken) =>
        await _sqlConnectionFactory.Execute(async connection =>
        {
            var result = (await connection.QueryAsync<ThreadDto>("SELECT * FROM Threads WHERE Id = @Id", new { Id = identifier })).ToList();

            if (result.Count == 0)
            {
                return None.Default;
            }

            if (result.Count > 1)
            {
                throw new InvalidOperationException("Received invalid count");
            }

            var threadDto = result.Single();

            var events = (await connection.QueryAsync<NumberedEventDto>(
            """
            SELECT *
            FROM ThreadEvents 
            WHERE ThreadId = @ThreadId
            """,
            new { ThreadId = identifier })).OrderBy(m => m.MessageNumber);

            var thread = Thread.Rehydrate(
                identifier: identifier,
                createdOn: threadDto.CreatedOn,
                title: threadDto.Title,
                persistedVersion: threadDto.Version,
                events: events.Select(EventDto.ToModel).ToArray());

            return thread.Some();
        }, cancellationToken);

    public async Task<IReadOnlyList<ThreadInfo>> ListThreads(CancellationToken cancellationToken) =>
      await _sqlConnectionFactory.Execute<IReadOnlyList<ThreadInfo>>(static async connection =>
      {
          return (await connection.QueryAsync<ThreadDto>("SELECT * FROM Threads")).Select(ThreadDto.ToModel).ToList();
      }, cancellationToken);

    public async Task<Option<QueryResult>> QueryMessages(ThreadIdentifier identifier, Cursor cursor, CancellationToken cancellationToken)
    {
        return await ThreadExists(identifier, cancellationToken) switch
        {
            false => None.Default,
            true => await QueryMessages().Some()
        };

        async Task<List<NumberedEventDto>> GetLastMessages(int limit)
        {
            return await _sqlConnectionFactory.Execute(async connection =>
            {
                return await connection.QueryAsync<NumberedEventDto>(
                """
                SELECT *
                FROM ThreadEvents 
                WHERE ThreadId = @ThreadId 
                ORDER BY MessageNumber 
                DESC LIMIT @Limit
                """,
                 new
                 {
                     ThreadId = identifier,
                     Limit = limit
                 }).ToList();
            }, cancellationToken);
        }

        async Task<List<NumberedEventDto>> GetSpecificMessages(int limit, int reference)
        {
            var from = reference;
            var messagesToFetch = limit + 1; // we fetch one more message to check if theres a next page available
            var to = reference + messagesToFetch - 1;
            return await _sqlConnectionFactory.Execute(async connection =>
            {
                return await connection.QueryAsync<NumberedEventDto>(
                 """
                 SELECT *
                 FROM ThreadEvents 
                 WHERE ThreadId = @ThreadId AND MessageNumber BETWEEN @From AND @To
                 """,
                 new
                 {
                     ThreadId = identifier,
                     From = from,
                     To = to
                 }).ToList();
            }, cancellationToken);
        }

        async Task<QueryResult> QueryMessages()
        {
            var limit = cursor.OverrideLimit switch
            {
                Some<int> some => some.Value,
                _ => cursor.Limit
            };

            var dtos = cursor.Reference switch
            {
                Some<int> some => await GetSpecificMessages(limit: limit, reference: some),
                _ => await GetLastMessages(limit)
            };

            if (dtos.Count == 0)
            {
                return QueryResult.Empty();
            }

            // we have tried to fetch (limit+1) and if we have succeeded, this means, we have a next page available
            var isNextAvailable = dtos.Count == limit + 1;

            var messages = dtos.OrderBy(m => m.MessageNumber)
               .Take(limit)
               .Select(dto => Message.FromModel(EventDto.ToModel(dto), dto.MessageNumber))
               .ToList();

            Cursor self = Cursor.Create(limit: cursor.Limit, reference: messages[0].Number)
                .DoIf(cursor => messages.Count < cursor.Limit, cursor => cursor.WithOneOffLimitOverride(messages.Count));

            var prevReference = Math.Max(1, messages[0].Number - cursor.Limit);
            var prevSize = messages[0].Number - prevReference;

            Option<Cursor> prev = messages[0].Number == 1 ?
                None.Default :
                Cursor.Create(limit: cursor.Limit, reference: prevReference)
                .DoIf(cursor => prevSize < cursor.Limit, cursor => cursor.WithOneOffLimitOverride(prevSize));

            Option<Cursor> next = isNextAvailable ?
                 Cursor.Create(limit: cursor.Limit, reference: messages[^1].Number + 1) :
                 None.Default;

            return new(messages, self, prev, next);
        }
    }

    private async Task<UpdateResult> UpdateThread(Thread thread, DbConnection connection, CancellationToken cancellationToken)
    {
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var rowsAffected = await connection.ExecuteAsync(
             "UPDATE Threads SET Title = @Title, ModifiedOn = @ModifiedOn, Version = @NewVersion where Id = @Id AND Version = @OldVersion",
                 new
                 {
                     Id = thread.Id,
                     Title = thread.Title,
                     ModifiedOn = thread.ModifiedOn,
                     NewVersion = thread.PendingVersion,
                     OldVersion = thread.PersistedVersion.ValueOrFailure
                 });

        if (rowsAffected == 0)
        {
            return new ConcurrencyConflict();
        }

        if (rowsAffected != 1)
        {
            throw new InvalidOperationException($"Expected rowsAffected 0 or 1 but got {rowsAffected}");
        }

        await SaveMessages(thread, connection, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        thread.MarkAggregateAsSynchronized();
        return new Updated();
    }

    private async Task<UpdateResult> CreateThread(Thread thread, DbConnection connection, CancellationToken cancellationToken)
    {
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        int rowsAffected;
        try
        {
            rowsAffected = await connection.ExecuteAsync("INSERT INTO Threads VALUES(@Id, @Title, @CreatedOn, @ModifiedOn, @Version)", ThreadDto.FromModel(thread));
        }
        catch (SqliteException sqlException)
        when (sqlException.SqliteExtendedErrorCode == SqliteExtendedErrorCodes.SQLITE_CONSTRAINT_PRIMARYKEY)
        {
            return new ConcurrencyConflict();
        }

        if (rowsAffected != 1)
        {
            throw new InvalidOperationException($"Expected rowsAffected 1 but got {rowsAffected}");
        }

        await SaveMessages(thread, connection, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        thread.MarkAggregateAsSynchronized();

        return new Created();
    }
    public async Task<UpdateResult> SaveThread(Thread thread, CancellationToken cancellationToken) =>
        await _sqlConnectionFactory.Execute(async connection =>
        {
            // No changes
            if (thread.PersistedVersion == thread.PendingVersion)
            {
                return new NoChanges();
            }

            return thread.PersistedVersion switch
            {
                Some<int> _ => await UpdateThread(thread, connection, cancellationToken),
                _ => await CreateThread(thread, connection, cancellationToken)
            };

        }, cancellationToken);

    private async Task SaveMessages(Thread thread, DbConnection connection, CancellationToken cancellationToken)
    {
        if (thread.PendingEvents.Count == 0)
        {
            return;
        }

        var messageNumber = thread.PersistedVersion switch { Some<int> some => some.Value, _ => 0 };
        foreach (var @event in thread.PendingEvents)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await InsertEvent(thread.Id, connection, ++messageNumber, @event);
        }
    }

    private async Task InsertEvent(ThreadIdentifier threadId, DbConnection connection, int messageNumber, MessageAdded @event)
    {
        var rowsAffected = await connection.ExecuteAsync(
                        """
                INSERT INTO ThreadEvents(ThreadId, MessageId, EventType, Content, RepliedToMessageId, CreatedOn, MessageNumber) 
                VALUES(@ThreadId, @MessageId, @EventType, @Content, @RepliedToMessageId, @CreatedOn, @MessageNumber)
                """,
                       NumberedEventDto.FromEventDto(EventDto.FromModel(threadId, @event), messageNumber));

        if (rowsAffected != 1)
        {
            _logger.Error("Could not persist message {MessageId} for thread {ThreadId}", @event.MessageId, threadId);
            throw new InvalidOperationException($"Expected rowsAffected 1 but got {rowsAffected}");
        }
    }

    public async Task<SuccessOrNotFound> RefreshThread(Thread thread, CancellationToken cancellationToken)
    {
        if (thread.PendingEvents.Count > 0)
        {
            throw new InvalidOperationException($"""
            Cannot refresh a thread with pending changes. Persist changes with {nameof(SaveThread)} first, 
            or discard them with {nameof(Thread)}.{nameof(Thread.DiscardPendingChanges)}
            """);
        }

        if (!thread.PersistedVersion.HasValue)
        {
            throw new InvalidOperationException("Cannot refresh a thread with no PersistedVersion");
        }

        if (thread.Messages.Count == 0)
        {
            throw new InvalidOperationException($"Cannot refresh a thread with no Messages. Use {nameof(GetThread)} instead.");
        }

        return await _sqlConnectionFactory.Execute<SuccessOrNotFound>(
            async connection =>
            {
                var events = (await connection.QueryAsync<NumberedEventDto>(
                """
                SELECT *
                FROM ThreadEvents 
                WHERE ThreadId = @ThreadId AND MessageNumber >= Version
                """,
                new
                {
                    ThreadId = thread.Id,
                    Version = thread.PersistedVersion.ValueOrFailure
                })).OrderBy(m => m.MessageNumber).ToList();

                if (events.Count == 0)
                {
                    return new NotFound();
                }

                if (events.Count == 1)
                {
                    return new Success();
                }

                thread.ForEach(events[1..].Select(EventDto.ToModel), (t, e) => t.Apply(e));
                thread.MarkAggregateAsSynchronized();

                return new Success();
            }, cancellationToken);
    }

    public async Task<Option<int>> AppendEvent<T>(ThreadIdentifier identifier, T messageAdded, CancellationToken cancellationToken)
        where T : MessageAdded
    {
        return await _sqlConnectionFactory.ExecuteAsTransaction<Option<int>>(async connection =>
        {
            var versionOrNone = await connection.QueryAsync<int>(
             """
             UPDATE Threads
             SET Version = Version + 1
             WHERE Id = @ThreadId
             RETURNING Version
            """, new { ThreadId = identifier }).SingleOrNone();

            if (versionOrNone is Some<int> version)
            {
                await InsertEvent(identifier, connection, version, messageAdded);
                return version;
            }
            else
            {
                return None.Default;
            }
        }, cancellationToken);
    }
}