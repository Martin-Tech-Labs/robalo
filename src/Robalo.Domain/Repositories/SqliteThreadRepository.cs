using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using Dapper;
using Microsoft.Data.Sqlite;
using Robalo.Common.Extensions;
using Robalo.Common.Models;
using Robalo.Domain.Events;
using Robalo.Domain.Models;
using Robalo.Domain.Repositories.Dto;
using Serilog;
using SQLitePCL;

namespace Robalo.Domain.Repositories;

public sealed class SqliteThreadRepository : IThreadRepository
{
    readonly ILogger _logger;
    readonly ISqlConnectionFactory _sqlConnectionFactory;

    public SqliteThreadRepository(ILogger logger, ISqlConnectionFactory sqlConnectionFactory)
    {
        _logger = logger.ForContext(GetType());
        _sqlConnectionFactory = sqlConnectionFactory;

        SqlMapper.RemoveTypeMap(typeof(DateTimeOffset));
        SqlMapper.AddTypeHandler(DateTimeOffsetTypeHandler.Default);
        SqlMapper.AddTypeHandler(VersionTypeHandler.Default);
        SqlMapper.AddTypeHandler(ThreadIdentifierTypeHandler.Default);
    }
    public async Task<SuccessOrNotFound> DeleteThread(ThreadIdentifier identifier, CancellationToken cancellationToken) =>
        await _sqlConnectionFactory.Execute<SuccessOrNotFound>(async connection =>
         {
             await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
             var rowsAffected = await connection.ExecuteAsync("DELETE FROM Threads WHERE Id = @ThreadId", new { ThreadId = identifier });

             if (rowsAffected == 0)
             {
                 return new NotFound();
             }

             if (rowsAffected != 1)
             {
                 throw new InvalidOperationException($"Returned rows affected {rowsAffected}, but expected 0 or 1");
             }

             rowsAffected = await connection.ExecuteAsync("DELETE FROM Messages WHERE ThreadId = @ThreadId", new { ThreadId = identifier });

             await transaction.CommitAsync(cancellationToken);

             return new Success();

         }, cancellationToken);

    public async Task<Option<Message>> GetMessage(ThreadIdentifier identifier, int messageNumber, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(messageNumber, 1);

        return await _sqlConnectionFactory.Execute(async connection =>
        {
            return (await connection.QueryAsync<MessageDto>("SELECT * FROM Messages where ThreadId = @ThreadId AND MessageNumber = @MessageNumber",
                new
                {
                    ThreadId = identifier,
                    MessageNumber = messageNumber
                })).SingleOrNone(MessageDto.ToModel);

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

            var messages = (await connection.QueryAsync<MessageDto>("SELECT * FROM Messages WHERE ThreadId = @ThreadId", new { ThreadId = identifier })).OrderBy(m => m.MessageNumber);

            var thread = Thread.Rehydrate(
                identifier: identifier,
                createdOn: threadDto.CreatedOn,
                title: threadDto.Title,
                version: threadDto.Version,
                events: messages.Select(m => new UserMessageAdded(m.Content, m.CreatedOn)));

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

        async Task<List<MessageDto>> GetLastMessages(int limit)
        {
            return await _sqlConnectionFactory.Execute(async connection =>
            {
                return await connection.QueryAsync<MessageDto>(
                 "SELECT * FROM Messages WHERE ThreadId = @ThreadId ORDER BY MessageNumber DESC LIMIT @Limit",
                 new
                 {
                     ThreadId = identifier,
                     Limit = limit
                 }).ToList();
            }, cancellationToken);
        }

        async Task<List<MessageDto>> GetSpecificMessages(int limit, int reference)
        {
            var from = reference;
            var messagesToFetch = limit + 1; // we fetch one more message to check if theres a next page available
            var to = reference + messagesToFetch - 1;
            return await _sqlConnectionFactory.Execute(async connection =>
            {
                return await connection.QueryAsync<MessageDto>(
                 "SELECT * FROM Messages WHERE ThreadId = @ThreadId AND MessageNumber BETWEEN @From AND @To",
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

            var messages = dtos.OrderBy(m => m.MessageNumber).Take(limit).Select(MessageDto.ToModel).ToList();

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
                     NewVersion = thread.NewVersion,
                     OldVersion = thread.OldVersion.ValueOrFailure
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
            if (thread.OldVersion == thread.NewVersion)
            {
                return new NoChanges();
            }

            return thread.OldVersion switch
            {
                Some<Version> _ => await UpdateThread(thread, connection, cancellationToken),
                _ => await CreateThread(thread, connection, cancellationToken)
            };

        }, cancellationToken);

    private async Task SaveMessages(Thread thread, DbConnection connection, CancellationToken cancellationToken)
    {
        if (thread.PendingEvents.Count == 0)
        {
            return;
        }

        foreach (var message in thread.Messages.ToList()[^(thread.PendingEvents.Count)..^0])
        {
            cancellationToken.ThrowIfCancellationRequested();

            var rowsAffected = await connection.ExecuteAsync(
                "INSERT INTO Messages VALUES(@ThreadId, @MessageNumber, @Source, @Content, @CreatedOn)",
                MessageDto.FromModel(thread.Id, message));

            if (rowsAffected != 1)
            {
                _logger.Error("Could not persist message {MessageNumber} for thread {ThreadId}", message.Number, thread.Id);
                throw new InvalidOperationException($"Expected rowsAffected 1 but got {rowsAffected}");
            }
        }
    }
}