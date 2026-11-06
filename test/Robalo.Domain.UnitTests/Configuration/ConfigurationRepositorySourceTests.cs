using Microsoft.Extensions.Configuration;
using Moq;
using Robalo.Common.Extensions;
using Robalo.Common.Models;
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
            .Add(new ConfigurationRepositorySource(_repository.Object))
            .Build();

        var options = configuration.GetSection("Robalo:Llm").Get<LlmOptions>();
        options.ShouldNotBeNull();
        options.Verify();

        options.ShouldBeEquivalentTo(expected.Value.LlmOptions);
    }


    [Fact]
    public void ShouldThrowExceptionOnMissingConfiguration()
    {
        var config = _fixture.Create<RobaloConfiguration>();
        config.Value.LlmOptions!.Endpoint = null;
        config.Value.Verify();

        _repository.Setup(s => s.LoadConfiguration(It.IsAny<CancellationToken>())).ReturnsAsync(config);
        _repository.Setup(s => s.LoadConfiguration(It.IsAny<CancellationToken>())).ReturnsAsync(None.Default);

        Should.Throw<Exception>(() => new ConfigurationBuilder()
            .Add(new ConfigurationRepositorySource(_repository.Object))
            .Build());
    }

    [Fact]
    public void ShouldThrowExceptionOnInvalidConfiguration()
    {

        _repository.Setup(s => s.LoadConfiguration(It.IsAny<CancellationToken>())).ReturnsAsync(None.Default);

        Should.Throw<Exception>(() => new ConfigurationBuilder()
            .Add(new ConfigurationRepositorySource(_repository.Object))
            .Build());
    }
}