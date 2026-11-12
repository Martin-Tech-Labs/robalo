using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapper;
using Robalo.Common.Extensions;
using Robalo.Common.Models;
using Robalo.Domain.Configuration;
using Robalo.Domain.Repositories.Sqlite.Dto;

namespace Robalo.Domain.Repositories.Sqlite;

public sealed class SqliteConfigurationRepository : IConfigurationRepository
{
    readonly ILogger _logger;
    readonly ISqliteConnectionFactory _connectionFactory;

    public SqliteConfigurationRepository(ILogger logger, ISqliteConnectionFactory connectionFactory)
    {
        _logger = logger.ForContext(GetType());
        _connectionFactory = connectionFactory;
    }
    public async Task<Option<RobaloConfiguration>> LoadConfiguration(CancellationToken cancellationToken)
    {
        return await _connectionFactory.Execute<Option<RobaloConfiguration>>(async connection =>
        {
            var result = (await connection.QueryAsync<ConfigurationDto>(
            """
            SELECT * 
            FROM Configurations
            ORDER BY ConfigurationId DESC
            LIMIT 1
            """)).ToList();

            if (result.Count == 0)
            {
                return None.Default;
            }

            if (result.Count > 1)
            {
                throw new InvalidOperationException("Received invalid count");
            }

            if (string.IsNullOrWhiteSpace(result[0].Value))
            {
                _logger.Warning("Configuration {ConfigurationId} returned empty Value", result[0].ConfigurationId);
                return new RobaloConfiguration(result[0].ConfigurationId, result[0].CreatedOn, new RobaloOptions(LlmOptions: null));
            }

            try
            {
                var robaloOptions = result[0].Value.DeserializeAsCamelCase<RobaloOptions>();

                if (robaloOptions is null)
                {
                    _logger.Warning("Configuration {ConfigurationId} deserializatin produced null", result[0].ConfigurationId);
                    return new RobaloConfiguration(result[0].ConfigurationId, result[0].CreatedOn, new RobaloOptions(LlmOptions: null));
                }

                return new RobaloConfiguration(result[0].ConfigurationId, result[0].CreatedOn, robaloOptions);
            }
            catch (JsonException exc)
            {
                _logger.Error(exc, "Configuration {ConfigurationId} deserialization produced error", result[0].ConfigurationId);
                return new RobaloConfiguration(result[0].ConfigurationId, result[0].CreatedOn, new RobaloOptions(LlmOptions: null));
            }
        }, cancellationToken);
    }

    public async Task<int> SaveConfiguration(RobaloOptions value, DateTimeOffset createdOn, CancellationToken cancellationToken)
    {
        return await _connectionFactory.Execute(async connection =>
        {
            var result = (await connection.QueryAsync<int>(
             """
            INSERT INTO Configurations(CreatedOn, Value)
            VALUES(@CreatedOn, @Value)
            RETURNING ConfigurationId
            """, new
             {
                 CreatedOn = createdOn,
                 Value = value.SerializeAsCamelCase()
             })).ToList();

            if (result.Count != 1)
            {
                throw new InvalidOperationException("Received invalid count");
            }

            return result[0];
        }, cancellationToken);
    }
}