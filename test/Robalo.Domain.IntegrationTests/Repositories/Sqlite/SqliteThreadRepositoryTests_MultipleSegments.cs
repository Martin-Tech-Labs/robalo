using Moq;
using Robalo.Common.Coordination;
using Robalo.Common.Extensions;
using Robalo.Common.Models;
using Robalo.Domain.Models;
using Robalo.Domain.Models.Events;
using Robalo.Domain.Repositories.Sqlite;
using Robalo.Domain.Repositories.Sqlite.Dto;
using Robalo.Tests.Common;
using Serilog;

namespace Robalo.Domain.IntegrationTests.Repositories.Sqlite;

public class SqliteThreadRepositoryTests_MultipleSegments : IClassFixture<DatabaseFixture>
{
    readonly SqliteThreadRepository _sut;
    readonly Fixture _fixture = new();
    readonly DateTimeOffset _utcNow = DateTimeOffset.UtcNow;
    readonly Mock<IMessageBus> _messageBus = new();

    public SqliteThreadRepositoryTests_MultipleSegments(DatabaseFixture fixture)
    {
        _sut = new SqliteThreadRepository(
            Log.Logger,
            new SqliteConnectionFactory(Log.Logger, new(fixture.Database, fixture.Password)),
            _messageBus.Object);

        Extensions.AddDapperTypeHandlers();
    }

    [Fact]
    public async Task ShouldCorrectly_Save_And_Load_BreakMessage_WithEmptySegment()
    {
        var thread = Thread.NewThread(_utcNow)
            .AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(1))
            .AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(2));

        thread.AddAssistantMessage(_fixture.Create<string>(), thread.Context[0].MessageId, _fixture.Create<int>(), _fixture.Create<int>(), _fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1));

        thread.ExtendedContext.ShouldBeEmpty();
        await _sut.SaveThread(thread, TestContext.Current.CancellationToken);
        var contextBeforeBreak = thread.Context.ToList();
        var content = _fixture.Create<string>();
        thread.AddBreakMessage(content, thread.ModifiedOn.AddSeconds(1));
        var messageId = thread.PendingEvents[^1].MessageId;
        thread.ExtendedContext.ToList().ShouldBeEquivalentTo(contextBeforeBreak);
        await _sut.SaveThread(thread, TestContext.Current.CancellationToken);
        thread.ExtendedContext.ShouldBeEmpty();

        var thread2 = (await _sut.LoadThread(thread.Id, TestContext.Current.CancellationToken)).ValueOrFailure;
        thread2.ShouldBeEquivalentTo(thread);

        var breakMessage = await _sut.GetMessage(messageId, TestContext.Current.CancellationToken);
        breakMessage.HasValue.ShouldBeTrue();
        breakMessage.ValueOrFailure.Content.ShouldBe(content);
        breakMessage.ValueOrFailure.CreatedOn.ShouldBe(_utcNow.AddSeconds(4));
        breakMessage.ValueOrFailure.EventType.ShouldBe(nameof(BreakMessageAdded));
    }

    [Fact]
    public async Task ShouldCorrectly_Save_And_Load_BreakMessage_WithNonEmptySegment()
    {
        var thread = Thread.NewThread(_utcNow)
            .AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(1))
            .AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(2));

        thread.AddAssistantMessage(_fixture.Create<string>(), thread.Context[0].MessageId, _fixture.Create<int>(), _fixture.Create<int>(), _fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1));

        thread.ExtendedContext.ShouldBeEmpty();
        await _sut.SaveThread(thread, TestContext.Current.CancellationToken);
        var contextBeforeBreak = thread.Context.ToList();
        thread.AddBreakMessage(_fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1));
        thread.ExtendedContext.ToList().ShouldBeEquivalentTo(contextBeforeBreak);
        thread.AddUserMessage(_fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1));
        thread.AddUserMessage(_fixture.Create<string>(), thread.ModifiedOn.AddSeconds(2));
        thread.AddAssistantMessage(_fixture.Create<string>(), thread.Context[0].MessageId, _fixture.Create<int>(), _fixture.Create<int>(), _fixture.Create<string>(), thread.ModifiedOn.AddSeconds(3));
        await _sut.SaveThread(thread, TestContext.Current.CancellationToken);
        thread.ExtendedContext.ShouldBeEmpty();

        var thread2 = (await _sut.LoadThread(thread.Id, TestContext.Current.CancellationToken)).ValueOrFailure;
        thread2.ShouldBeEquivalentTo(thread);
    }

    [Fact]
    public async Task ShouldCorrectly_Save_And_Load_SummaryMessage()
    {
        var thread = Thread.NewThread(_utcNow)
            .AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(1))
            .AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(2));

        thread.AddAssistantMessage(_fixture.Create<string>(), thread.Context[0].MessageId, _fixture.Create<int>(), _fixture.Create<int>(), _fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1));

        thread.ExtendedContext.ShouldBeEmpty();
        await _sut.SaveThread(thread, TestContext.Current.CancellationToken);
        var contextBeforeBreak = thread.Context.ToList();
        thread.AddBreakMessage(_fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1));
        thread.ExtendedContext.ToList().ShouldBeEquivalentTo(contextBeforeBreak);

        var content = _fixture.Create<string>();
        var tokens = _fixture.Create<int>();
        var model = _fixture.Create<string>();
        var references = thread.ExtendedContext.Select(c => c.MessageId).ToList();

        thread.AddSummaryMessage(content, thread.ExtendedContext.Select(c => c.MessageId).ToList(), tokens, model, thread.ModifiedOn.AddSeconds(1));
        var messageId = thread.PendingEvents[^1].MessageId;
        thread.AddUserMessage(_fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1));
        thread.AddUserMessage(_fixture.Create<string>(), thread.ModifiedOn.AddSeconds(2));
        thread.AddAssistantMessage(_fixture.Create<string>(), thread.Context[0].MessageId, _fixture.Create<int>(), _fixture.Create<int>(), _fixture.Create<string>(), thread.ModifiedOn.AddSeconds(3));
        await _sut.SaveThread(thread, TestContext.Current.CancellationToken);
        thread.ExtendedContext.ShouldBeEmpty();

        var thread2 = (await _sut.LoadThread(thread.Id, TestContext.Current.CancellationToken)).ValueOrFailure;
        Assert.Equivalent(thread, thread2, strict: true);
        thread.Context.Select(c => c.MessageId).ShouldBeEquivalentTo(thread2.Context.Select(c => c.MessageId));

        var summaryMessage = await _sut.GetMessage(messageId, TestContext.Current.CancellationToken);
        summaryMessage.HasValue.ShouldBeTrue();
        summaryMessage.ValueOrFailure.Content.ShouldBe(content);
        summaryMessage.ValueOrFailure.Tokens.ShouldBe(tokens);
        summaryMessage.ValueOrFailure.Model.ShouldBe(model);
        summaryMessage.ValueOrFailure.EventType.ShouldBe(nameof(SummaryMessageAdded));
        summaryMessage.ValueOrFailure.References.HasValue.ShouldBeTrue();
        summaryMessage.ValueOrFailure.References.ValueOrFailure.ToList().ShouldBeEquivalentTo(references);
    }

    [Fact]
    public async Task RefreshThread_ShouldCorrectly_Handle_Segments()
    {
        var thread = Thread.NewThread(_utcNow)
            .AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(1))
            .AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(2));
        thread.AddAssistantMessage(_fixture.Create<string>(), thread.Context[0].MessageId, _fixture.Create<int>(), _fixture.Create<int>(), _fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1));
        await _sut.SaveThread(thread, TestContext.Current.CancellationToken);
        var thread2 = (await _sut.LoadThread(thread.Id, TestContext.Current.CancellationToken)).ValueOrFailure;
        thread2.AddBreakMessage(_fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1));
        await _sut.SaveThread(thread2, TestContext.Current.CancellationToken);
        await _sut.RefreshThread(thread, TestContext.Current.CancellationToken);
        thread.ExtendedContext.ShouldBeEmpty();
        thread.Context.ShouldBeEmpty();
        thread.IsAnswered.ShouldBeTrue();
        thread.ModifiedOn.ShouldBe(thread2.ModifiedOn);
        Assert.Equivalent(thread2, thread, strict: true);
        thread2.Context.Select(c => c.MessageId).ShouldBeEquivalentTo(thread.Context.Select(c => c.MessageId));
        var userContent = _fixture.Create<string>();
        thread2.AddUserMessage(userContent, thread2.ModifiedOn.AddSeconds(1));
        var assistantContent = _fixture.Create<string>();
        var assistantTokens = _fixture.Create<int>();
        var assistantModel = _fixture.Create<string>();
        var repliedToMessageId = thread2.Context[0].MessageId;
        thread2.AddAssistantMessage(assistantContent, repliedToMessageId, _fixture.Create<int>(), assistantTokens, assistantModel, thread2.ModifiedOn.AddSeconds(1));
        await _sut.SaveThread(thread2, TestContext.Current.CancellationToken);
        thread2.AddBreakMessage(_fixture.Create<string>(), thread2.ModifiedOn.AddSeconds(1));
        var summaryContent = _fixture.Create<string>();
        var summaryTokens = _fixture.Create<int>();
        var summaryModel = _fixture.Create<string>();
        var summaryReferences = thread2.ExtendedContext.Select(m => m.MessageId).ToList();
        thread2.AddSummaryMessage(summaryContent, summaryReferences, summaryTokens, summaryModel, thread2.ModifiedOn.AddSeconds(1));
        foreach (var msg in thread2.ExtendedContext)
        {
            thread2.AddCopyOfMessage(msg, thread2.ModifiedOn.AddSeconds(1));
        }

        await _sut.SaveThread(thread2, TestContext.Current.CancellationToken);
        thread2.AddBreakMessage(_fixture.Create<string>(), thread2.ModifiedOn.AddSeconds(1));
        foreach (var msg in thread2.ExtendedContext)
        {
            thread2.AddCopyOfMessage(msg, thread2.ModifiedOn.AddSeconds(1));
        }

        var summaryCopyMessageId = thread2.PendingEvents[1].MessageId;
        var userCopyMessageId = thread2.PendingEvents[2].MessageId;
        var assistentCopyMessageId = thread2.PendingEvents[3].MessageId;

        await _sut.SaveThread(thread2, TestContext.Current.CancellationToken);
        await _sut.RefreshThread(thread, TestContext.Current.CancellationToken);
        Assert.Equivalent(thread2, thread, strict: true);
        thread2.Context.Select(c => c.MessageId).ShouldBeEquivalentTo(thread.Context.Select(c => c.MessageId));

        var summaryCopyMessage = await _sut.GetMessage(summaryCopyMessageId, TestContext.Current.CancellationToken);
        summaryCopyMessage.HasValue.ShouldBeTrue();
        summaryCopyMessage.ValueOrFailure.EventType.ShouldBe(nameof(SummaryMessageCopied));
        summaryCopyMessage.ValueOrFailure.Content.ShouldBe(summaryContent);
        summaryCopyMessage.ValueOrFailure.Model.ShouldBe(summaryModel);
        summaryCopyMessage.ValueOrFailure.Tokens.ShouldBe(summaryTokens);
        summaryCopyMessage.ValueOrFailure.References.HasValue.ShouldBeTrue();
        summaryCopyMessage.ValueOrFailure.References.ValueOrFailure.ToList().ShouldBeEquivalentTo(summaryReferences);

        var userCopyMessage = await _sut.GetMessage(userCopyMessageId, TestContext.Current.CancellationToken);
        userCopyMessage.HasValue.ShouldBeTrue();
        userCopyMessage.ValueOrFailure.EventType.ShouldBe(nameof(UserMessageCopied));
        userCopyMessage.ValueOrFailure.Content.ShouldBe(userContent);

        var assistantCopyMessage = await _sut.GetMessage(assistentCopyMessageId, TestContext.Current.CancellationToken);
        assistantCopyMessage.HasValue.ShouldBeTrue();
        assistantCopyMessage.ValueOrFailure.EventType.ShouldBe(nameof(AssistantMessageCopied));
        assistantCopyMessage.ValueOrFailure.Content.ShouldBe(assistantContent);
        assistantCopyMessage.ValueOrFailure.Tokens.ShouldBe(assistantTokens);
        assistantCopyMessage.ValueOrFailure.Model.ShouldBe(assistantModel);
        assistantCopyMessage.ValueOrFailure.PromptTokens.ShouldBeNone();
    }

    [Fact]
    public async Task ComplexTest_With_Save_Load_and_Get_Messages()
    {
        var events = new List<MessageAdded>();
        var thread = Thread.NewThread(_utcNow)
            .AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(1))
            .AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(2));

        thread.AddAssistantMessage(_fixture.Create<string>(), thread.Context[0].MessageId, _fixture.Create<int>(), _fixture.Create<int>(), _fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1));

        thread.ExtendedContext.ShouldBeEmpty();
        events.AddRange(thread.PendingEvents);
        await _sut.SaveThread(thread, TestContext.Current.CancellationToken);
        var contextBeforeBreak = thread.Context.ToList();
        thread.AddBreakMessage(_fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1));
        thread.ExtendedContext.ToList().ShouldBeEquivalentTo(contextBeforeBreak);
        thread.AddSummaryMessage(_fixture.Create<string>(), thread.ExtendedContext.Select(c => c.MessageId).ToList(), _fixture.Create<int>(), _fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1));
        thread.AddCopyOfMessage(thread.ExtendedContext[0], thread.ModifiedOn.AddSeconds(1));
        thread.AddCopyOfMessage(thread.ExtendedContext[1], thread.ModifiedOn.AddSeconds(1));
        thread.AddCopyOfMessage(thread.ExtendedContext[2], thread.ModifiedOn.AddSeconds(1));
        thread.AddUserMessage(_fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1));
        thread.AddUserMessage(_fixture.Create<string>(), thread.ModifiedOn.AddSeconds(2));
        thread.AddAssistantMessage(_fixture.Create<string>(), thread.ExtendedContext[0].MessageId, _fixture.Create<int>(), _fixture.Create<int>(), _fixture.Create<string>(), thread.ModifiedOn.AddSeconds(3));
        events.AddRange(thread.PendingEvents);
        await _sut.SaveThread(thread, TestContext.Current.CancellationToken);
        thread.ExtendedContext.ShouldBeEmpty();
        contextBeforeBreak = thread.Context.ToList();
        thread.AddBreakMessage(_fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1));
        thread.ExtendedContext.ToList().ShouldBeEquivalentTo(contextBeforeBreak);
        foreach (var msg in thread.ExtendedContext)
        {
            thread.AddCopyOfMessage(msg, thread.ModifiedOn.AddSeconds(1));
        }
        thread.AddSummaryMessage(_fixture.Create<string>(), new List<MessageIdentifier> { thread.ExtendedContext[0].MessageId }, _fixture.Create<int>(), _fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1));
        events.AddRange(thread.PendingEvents);
        await _sut.SaveThread(thread, TestContext.Current.CancellationToken);
        Should.Throw<InvalidOperationException>((() => thread.AddSummaryMessage(_fixture.Create<string>(), contextBeforeBreak.Select(c => c.MessageId).ToList(), _fixture.Create<int>(), _fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1))));
        Should.Throw<InvalidOperationException>((() => thread.AddCopyOfMessage(contextBeforeBreak[0], thread.ModifiedOn.AddSeconds(1))));
        Should.Throw<ArgumentException>((() => thread.AddAssistantMessage(_fixture.Create<string>(), contextBeforeBreak[0].MessageId, _fixture.Create<int>(), _fixture.Create<int>(), _fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1))));

        var thread2 = (await _sut.LoadThread(thread.Id, TestContext.Current.CancellationToken)).ValueOrFailure;
        Assert.Equivalent(thread, thread2, strict: true);
        thread.Context.Select(c => c.MessageId).ShouldBeEquivalentTo(thread2.Context.Select(c => c.MessageId));

        var messages = await (await _sut.GetMessages(thread.Id, TestContext.Current.CancellationToken)).ValueOrFailure.ToListAsync(TestContext.Current.CancellationToken);
        var i = 0;
        Assert.Equivalent(messages, events.Select(e => Message.FromModel(e, thread.Id, ++i)));
    }
}