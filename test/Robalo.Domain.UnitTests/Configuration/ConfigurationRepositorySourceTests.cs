using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Robalo.Common.Extensions;
using Robalo.Common.Models;
using Robalo.Common.Rop;
using Robalo.Domain.Configuration;
using Robalo.Domain.Repositories;

namespace Robalo.Domain.UnitTests;

public class ConfigurationRepositorySourceTests
{
    readonly Mock<IConfigurationRepository> _repository = new();
    readonly Fixture _fixture = new();

    [Fact]
    public void ShouldLoadValidConfiguration()
    {
        var expected = _fixture.Create<RobaloConfiguration>();
        expected.Value.Verify();

        _repository.Setup(s => s.LoadConfiguration(It.IsAny<CancellationToken>())).ReturnsAsync(expected);


        var configuration = new ConfigurationBuilder()
            .Add(new ConfigurationRepositorySource(_repository.Object, Timeout.InfiniteTimeSpan))
            .Build();

        var options = configuration.GetSection("Robalo:Llm").Get<LlmOptions>();
        options.ShouldNotBeNull();
        options.Verify();

        options.ShouldBeEquivalentTo(expected.Value.LlmOptions);
    }

    [Fact]
    public void OnReload_ShouldLoadNewConfigation()
    {
        var oldConfig = _fixture.Create<RobaloConfiguration>() with { ConfigurationId = 1 };
        oldConfig.Value.Verify();

        _repository.Setup(s => s.LoadConfiguration(It.IsAny<CancellationToken>())).ReturnsAsync(oldConfig);

        var configuration = new ConfigurationBuilder()
            .Add(new ConfigurationRepositorySource(_repository.Object, Timeout.InfiniteTimeSpan))
            .Build();

        var options = configuration.GetSection("Robalo:Llm").Get<LlmOptions>();
        options.ShouldNotBeNull();
        options.Verify();
        options.ShouldBeEquivalentTo(oldConfig.Value.LlmOptions);

        var newConfig = _fixture.Create<RobaloConfiguration>() with { ConfigurationId = 2 };
        newConfig.Value.Verify();

        _repository.Setup(s => s.LoadConfiguration(It.IsAny<CancellationToken>())).ReturnsAsync(newConfig);
        configuration.Reload();

        options = configuration.GetSection("Robalo:Llm").Get<LlmOptions>();
        options.ShouldNotBeNull();
        options.Verify();
        options.ShouldBeEquivalentTo(newConfig.Value.LlmOptions);
    }

    [Fact]
    public void OnReload_ShouldNorReloadConfigation_WithSameId()
    {
        var oldConfig = _fixture.Create<RobaloConfiguration>() with { ConfigurationId = 1 };
        oldConfig.Value.Verify();

        _repository.Setup(s => s.LoadConfiguration(It.IsAny<CancellationToken>())).ReturnsAsync(oldConfig);

        var configuration = new ConfigurationBuilder()
            .Add(new ConfigurationRepositorySource(_repository.Object, Timeout.InfiniteTimeSpan))
            .Build();

        var options = configuration.GetSection("Robalo:Llm").Get<LlmOptions>();
        options.ShouldNotBeNull();
        options.Verify();
        options.ShouldBeEquivalentTo(oldConfig.Value.LlmOptions);

        var newConfig = _fixture.Create<RobaloConfiguration>() with { ConfigurationId = 1 };
        newConfig.Value.Verify();

        _repository.Setup(s => s.LoadConfiguration(It.IsAny<CancellationToken>())).ReturnsAsync(newConfig);
        configuration.Reload();

        options = configuration.GetSection("Robalo:Llm").Get<LlmOptions>();
        options.ShouldNotBeNull();
        options.Verify();
        options.ShouldBeEquivalentTo(oldConfig.Value.LlmOptions);
    }

    [Fact]
    public void OnReload_ShouldNorReloadConfigation_WhenNoneReturned()
    {
        var oldConfig = _fixture.Create<RobaloConfiguration>() with { ConfigurationId = 1 };
        oldConfig.Value.Verify();

        _repository.Setup(s => s.LoadConfiguration(It.IsAny<CancellationToken>())).ReturnsAsync(oldConfig);

        var configuration = new ConfigurationBuilder()
            .Add(new ConfigurationRepositorySource(_repository.Object, Timeout.InfiniteTimeSpan))
            .Build();

        var options = configuration.GetSection("Robalo:Llm").Get<LlmOptions>();
        options.ShouldNotBeNull();
        options.Verify();
        options.ShouldBeEquivalentTo(oldConfig.Value.LlmOptions);

        _repository.Setup(s => s.LoadConfiguration(It.IsAny<CancellationToken>())).ReturnsAsync(None.Default);
        configuration.Reload();

        options = configuration.GetSection("Robalo:Llm").Get<LlmOptions>();
        options.ShouldNotBeNull();
        options.Verify();
        options.ShouldBeEquivalentTo(oldConfig.Value.LlmOptions);
    }

    [Fact]
    public void OnReload_ShouldNorReloadConfigation_WhenConfigurationIsInvalid()
    {
        var oldConfig = _fixture.Create<RobaloConfiguration>() with { ConfigurationId = 1 };
        oldConfig.Value.Verify();

        _repository.Setup(s => s.LoadConfiguration(It.IsAny<CancellationToken>())).ReturnsAsync(oldConfig);

        var configuration = new ConfigurationBuilder()
            .Add(new ConfigurationRepositorySource(_repository.Object, Timeout.InfiniteTimeSpan))
            .Build();

        var options = configuration.GetSection("Robalo:Llm").Get<LlmOptions>();
        options.ShouldNotBeNull();
        options.Verify();
        options.ShouldBeEquivalentTo(oldConfig.Value.LlmOptions);

        var newConfig = _fixture.Create<RobaloConfiguration>() with { ConfigurationId = 2 };
        newConfig.Value.Verify();
        newConfig.Value.LlmOptions.Endpoint = null;

        _repository.Setup(s => s.LoadConfiguration(It.IsAny<CancellationToken>())).ReturnsAsync(newConfig);
        configuration.Reload();

        options = configuration.GetSection("Robalo:Llm").Get<LlmOptions>();
        options.ShouldNotBeNull();
        options.Verify();
        options.ShouldBeEquivalentTo(oldConfig.Value.LlmOptions);
    }


    [Fact]
    public void ShouldThrowExceptionOnInvalidConfiguration()
    {
        var config = _fixture.Create<RobaloConfiguration>();
        config.Value.LlmOptions!.Endpoint = null;

        _repository.Setup(s => s.LoadConfiguration(It.IsAny<CancellationToken>())).ReturnsAsync(config);

        Should.Throw<Exception>(() => new ConfigurationBuilder()
            .Add(new ConfigurationRepositorySource(_repository.Object, Timeout.InfiniteTimeSpan))
            .Build());
    }

    [Fact]
    public void ShouldThrowExceptionOnMissingConfiguration()
    {
        _repository.Setup(s => s.LoadConfiguration(It.IsAny<CancellationToken>())).ReturnsAsync(None.Default);

        Should.Throw<Exception>(() => new ConfigurationBuilder()
            .Add(new ConfigurationRepositorySource(_repository.Object, Timeout.InfiniteTimeSpan))
            .Build());
    }

    [Fact]
    public async Task Polling_ShouldNotifyOptionsMonitor_WhenConfigurationChanges()
    {
        TimeSpan pollingInterval = TimeSpan.FromMilliseconds(50);
        var oldConfig = _fixture.Create<RobaloConfiguration>() with { ConfigurationId = 1 };
        oldConfig.Value.Verify();
        _repository.Setup(r => r.LoadConfiguration(It.IsAny<CancellationToken>())).ReturnsAsync(oldConfig);

        var configuration = new ConfigurationBuilder()
            .Add(new ConfigurationRepositorySource(
                _repository.Object,
                pollingInterval))
            .Build();

        var services = new ServiceCollection();
        services.AddOptions<LlmOptions>()
            .Bind(configuration.GetSection("Robalo:Llm"));

        using var serviceProvider = services.BuildServiceProvider();
        var monitor = serviceProvider.GetRequiredService<IOptionsMonitor<LlmOptions>>();

        monitor.CurrentValue.ShouldBeEquivalentTo(oldConfig.Value.LlmOptions);
        serviceProvider.GetRequiredService<IOptionsSnapshot<LlmOptions>>().Value.ShouldBeEquivalentTo(oldConfig.Value.LlmOptions, "old");

        var changed = new TaskCompletionSource<LlmOptions>(TaskCreationOptions.RunContinuationsAsynchronously);

        var newConfig = _fixture.Create<RobaloConfiguration>() with { ConfigurationId = 2 };
        newConfig.Value.Verify();
        _repository.Setup(r => r.LoadConfiguration(It.IsAny<CancellationToken>())).ReturnsAsync(newConfig);

        using var _ = monitor.OnChange((options, _) => changed.TrySetResult(options));

        var result = await changed.Task;
        result.ShouldBeEquivalentTo(newConfig.Value.LlmOptions);
        monitor.CurrentValue.ShouldBeEquivalentTo(result);
        using var scope = serviceProvider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<LlmOptions>>().Value.ShouldBeEquivalentTo(newConfig.Value.LlmOptions, "new");
    }
}