using Robalo.Domain.Models;
using Robalo.Domain.Repositories;
using Serilog;
using Robalo.Tests.Common;
using Robalo.Common.Models;
using Robalo.Common.Extensions;

namespace Robalo.Domain.UnitTests.Repositories;

public class InMemoryThreadRepositoryTests
{
    private readonly InMemoryThreadRepository _sut = new(Log.Logger);
    private readonly Fixture _fixture = new();

    [Fact]
    public void GetThread_ShouldThrowException_OnWrongId()
    {
        Should.Throw<ArgumentException>(() => _sut.GetThread(Identifier.NewMessageId(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetThread_ShouldReturnNone_OnNonExistingThread()
    {
        var result = await _sut.GetThread(Identifier.NewThreadId(), TestContext.Current.CancellationToken);
        result.ShouldBeNone();
    }

    [Fact]
    public async Task DeleteThread_ShouldThrowException_OnWrongId()
    {
        Should.Throw<ArgumentException>(() => _sut.DeleteThread(Identifier.NewMessageId(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteThread_ShouldReturnNotFound_OnNonExistingThread()
    {
        var result = await _sut.DeleteThread(Identifier.NewThreadId(), TestContext.Current.CancellationToken);
        result.Value.ShouldBeOfType<NotFound>();
    }

    [Fact]
    public async Task GetMessages_ShouldThrowException_OnWrongId()
    {
        Should.Throw<ArgumentException>(() => _sut.GetMessages(Identifier.NewMessageId(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetMessages_ShouldRetunNone_OnNonExistingThread()
    {
        var result = await _sut.GetMessages(Identifier.NewThreadId(), TestContext.Current.CancellationToken);
        result.ShouldBeNone();
    }

    [Fact]
    public async Task SaveChanges_ShouldSaveNewThread_WithNoMessages()
    {
        var thread = Thread.NewThread(Identifier.NewThreadId(), _fixture.Create<DateTimeOffset>());
        var result = await _sut.SaveThread(thread, TestContext.Current.CancellationToken);
        result.Value.ShouldBeOfType<Created>();

        thread.PendingEvents.ShouldBeEmpty();
        thread.OldVersion.HasValue.ShouldBeTrue();
        thread.OldVersion.ShouldBe(thread.NewVersion);
    }

    [Fact]
    public async Task GetThread_ShouldRetrieveThread_WithNoMessages()
    {
        var threadId = Identifier.NewThreadId();
        var createdOn = _fixture.Create<DateTimeOffset>();
        var thread1 = Thread.NewThread(threadId, createdOn);
        (await _sut.SaveThread(thread1, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Created>();

        var version = thread1.NewVersion;

        var threadOrNone = await _sut.GetThread(threadId, TestContext.Current.CancellationToken);
        threadOrNone.HasValue.ShouldBeTrue();

        var thread2 = threadOrNone.ValueOrFailure;

        thread2.ShouldNotBe(thread1);
        thread2.Title.ShouldBeNull();
        thread2.CreatedOn.ShouldBe(thread1.CreatedOn);
        thread2.NewVersion.ShouldBe(thread2.OldVersion);
        thread2.NewVersion.ShouldBe(version);

        thread2.PendingEvents.ShouldBeEmpty();
        thread2.Messages.ShouldBeEmpty();
    }

}