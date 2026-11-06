using Moq;
using Robalo.Common.Coordination;
using Robalo.Common.Extensions;
using Robalo.Domain.Models;
using Robalo.Domain.Models.Configuration;
using Robalo.Domain.Repositories.Sqlite;
using Robalo.Domain.Streaming;
using Robalo.Tests.Common;
using Serilog;
using Extensions = Robalo.Domain.Repositories.Sqlite.Extensions;

namespace Robalo.Domain.IntegrationTests.Streaming;

public class SqliteConfigurationRepositoryTests : IDisposable
{
    readonly SqliteConfigurationRepository _sut;
    readonly DatabaseFixture _databaseFixture = new();
    readonly DateTimeOffset _utcNow;
    readonly Fixture _fixture = new();

    public SqliteConfigurationRepositoryTests()
    {
        _sut = new SqliteConfigurationRepository(
            Log.Logger,
            new SqliteConnectionFactory(Log.Logger, new(_databaseFixture.Database, _databaseFixture.Password)));

        Extensions.AddDapperTypeHandlers();

        _utcNow = _fixture.Create<DateTimeOffset>();
    }

    [Fact]
    public async Task Load_Configuration_ShouldReturnNone_On_No_Configurations()
    {
        var result = await _sut.LoadConfiguration(TestContext.Current.CancellationToken);
        result.ShouldBeNone();
    }

    [Fact]
    public async Task SaveConfiguration_ShouldCorrentlySaveConfiguration()
    {
        var config = _fixture.Create<RobaloOptions>();
        config.Verify();
        var resultSave = await _sut.SaveConfiguration(config, _utcNow, TestContext.Current.CancellationToken);
        resultSave.ShouldBe(1);

        var resultLoad = await _sut.LoadConfiguration(TestContext.Current.CancellationToken);
        resultLoad.HasValue.ShouldBeTrue();
        var loadedConfig = resultLoad.ValueOrFailure;
        loadedConfig.ConfigurationId.ShouldBe(1);
        loadedConfig.CreatedOn.ShouldBe(_utcNow);
        loadedConfig.Value.ShouldBeEquivalentTo(config);
        loadedConfig.Value.Verify();
    }

    [Fact]
    public async Task LoadConfiguration_ShouldRetrieveTheLastConfiguration()
    {
        var number = _fixture.Create<int>();
        var configs = new List<RobaloOptions>();

        for (var i = 1; i <= number; i++)
        {
            var config = _fixture.Create<RobaloOptions>();
            config.Verify();
            configs.Add(config);
        }

        configs.DistinctBy(c => c.LlmOptions!.Endpoint).Count().ShouldBe(number);
        var j = 0;
        foreach (var config in configs)
        {
            (await _sut.SaveConfiguration(config, _utcNow.AddSeconds(++j), TestContext.Current.CancellationToken)).ShouldBe(j);
        }

        var resultLoad = await _sut.LoadConfiguration(TestContext.Current.CancellationToken);
        resultLoad.HasValue.ShouldBeTrue();
        var loadedConfig = resultLoad.ValueOrFailure;
        loadedConfig.ConfigurationId.ShouldBe(number);
        loadedConfig.CreatedOn.ShouldBe(_utcNow.AddSeconds(number));
        loadedConfig.Value.ShouldBeEquivalentTo(configs[^1]);
        loadedConfig.Value.Verify();
    }

    public void Dispose()
    {
        _databaseFixture.Dispose();
        GC.SuppressFinalize(this);
    }
}