#pragma warning disable CA1822

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mono.Cecil.Cil;
using Moq;
using Robalo.Common.Coordination;
using Robalo.Common.Rop;
using Robalo.Domain.Models;
using Robalo.Domain.Models.MessageBus;
using Serilog;

namespace Robalo.Common.UnitTests.Coordination;

public class MessageBusTests
{
    sealed class TestHandler
    {
        public void Handle(Action t) => t();
    }

    readonly MessageBus _sut;
    readonly Mock<IServiceProvider> _serviceProvider = new();
    readonly Fixture _fixture = new();

    public MessageBusTests()
    {
        _sut = new(Log.Logger, _serviceProvider.Object);
    }

    [Fact]
    public void On_Action_With_Single_Subscriber()
    {
        int result = -1;
        _sut.On<int>(i => result = i, out _);

        var expectedResult = _fixture.Create<int>();

        _sut.Send(expectedResult);

        result.ShouldBe(expectedResult);
    }

    [Fact]
    public void On_ServiceProvider_With_Single_Subscriber()
    {
        int result = -1;
        _sut.On<int, TestHandler>((i, h) => h.Handle(() => result = i), out var _);

        _serviceProvider
            .Setup(t => t.GetService(typeof(TestHandler)))
            .Returns(new TestHandler());

        var expectedResult = _fixture.Create<int>();

        _sut.Send(expectedResult);

        result.ShouldBe(expectedResult);
    }

    [Fact]
    public void On_ServiceProvider_And_Action_On_MultipleSubscribers()
    {
        int result1 = -1;
        int result2 = -1;
        int result3 = -1;
        _sut.On<int, TestHandler>((i, h) => h.Handle(() => result1 = i), out var _);
        _sut.On<int, TestHandler>((i, h) => h.Handle(() => result2 = i + 1), out var _);
        _sut.On<int>(i => result3 = i + 2, out var _);

        _serviceProvider
            .Setup(t => t.GetService(typeof(TestHandler)))
            .Returns(new TestHandler());

        var expectedResult = _fixture.Create<int>();

        _sut.Send(expectedResult);

        result1.ShouldBe(expectedResult);
        result2.ShouldBe(expectedResult + 1);
        result3.ShouldBe(expectedResult + 2);
    }

    [Fact]
    public void On_ServiceProvider_And_Action_On_MultipleSubscribers_And_Off()
    {
        int result1 = -1;
        int result2 = -1;
        int result3 = -1;
        _sut.On<int, TestHandler>((i, h) => h.Handle(() => result1 = i), out var _);
        _sut.On<int, TestHandler>((i, h) => h.Handle(() => result2 = i + 1), out var id);
        _sut.On<int>(i => result3 = i + 2, out var _);

        _sut.Off(id);

        _serviceProvider
            .Setup(t => t.GetService(typeof(TestHandler)))
            .Returns(new TestHandler());

        var expectedResult = _fixture.Create<int>();

        _sut.Send(expectedResult);

        result1.ShouldBe(expectedResult);
        result2.ShouldBe(-1);
        result3.ShouldBe(expectedResult + 2);
    }

    [Fact]
    public void On_ServiceProvider_And_Action_On_MultipleSubscribers_And_TestHander_NotResolved()
    {
        int result1 = -1;
        int result2 = -1;
        int result3 = -1;
        _sut.On<int, TestHandler>((i, h) => h.Handle(() => result1 = i), out var _);
        _sut.On<int, TestHandler>((i, h) => h.Handle(() => result2 = i + 1), out var id);
        _sut.On<int>(i => result3 = i + 2, out var _);

        var expectedResult = _fixture.Create<int>();

        _sut.Send(expectedResult);

        result1.ShouldBe(-1);
        result2.ShouldBe(-1);
        result3.ShouldBe(expectedResult + 2);
    }

    [Fact]
    public void On_ServiceProvider_And_Action_On_MultipleSubscribers_And_Exception()
    {
        int result1 = -1;
        int result2 = -1;
        int result3 = -1;
        int result4 = -1;
        _sut.On<int, TestHandler>((i, h) => h.Handle(() => throw new InvalidOperationException()), out var _);
        _sut.On<int, TestHandler>((i, h) => h.Handle(() => result2 = i), out var id);
        _sut.On<int>(i => throw new InvalidOperationException(), out var _);
        _sut.On<int>(i => result4 = i, out var _);

        _serviceProvider
            .Setup(s => s.GetService(typeof(TestHandler)))
            .Returns(new TestHandler());

        var expectedResult = _fixture.Create<int>();

        _sut.Send(expectedResult);

        result1.ShouldBe(-1);
        result2.ShouldBe(expectedResult);
        result3.ShouldBe(-1);
        result4.ShouldBe(expectedResult);
    }

    [Fact]
    public void On_Action_With_Multiple_Subscribers()
    {
        int result1 = -1;
        int result2 = -1;
        int result3 = -1;
        _sut.On<int>(i => result1 = i + 1, out _);
        _sut.On<int>(i => result2 = i + 2, out _);
        _sut.On<int>(i => result3 = i + 3, out _);

        var expectedResult = _fixture.Create<int>();

        _sut.Send(expectedResult);
        result1.ShouldBe(expectedResult + 1);
        result2.ShouldBe(expectedResult + 2);
        result3.ShouldBe(expectedResult + 3);
    }

    [Fact]
    public void On_Action_With_Multiple_Subscribers_And_DifferentEvents()
    {
        int resultInt1 = -1;
        int resultInt2 = -1;
        string resultString1 = "";
        string resultString2 = "Hi";

        ThreadUpdated? resultId3 = null;
        ThreadUpdated? resultId4 = new(ThreadIdentifier.NewIdentifier());
        _sut.On<int>(i => resultInt1 = i, out _);
        _sut.On<string>(i => resultString1 = i, out _);
        _sut.On<ThreadUpdated>(i => resultId3 = i, out _);

        _sut.On<int>(i => resultInt2 = i + 1, out _);
        _sut.On<string>(i => resultString2 += i, out _);
        _sut.On<ThreadUpdated>(i => resultId4 = i, out _);

        var expectedResultInt = _fixture.Create<int>();
        var expectedResultString = _fixture.Create<string>();
        var expectedResultId = ThreadIdentifier.NewIdentifier();

        _sut.Send(expectedResultInt);
        _sut.Send(expectedResultString);
        _sut.Send(new ThreadUpdated(expectedResultId));
        resultInt1.ShouldBe(expectedResultInt);
        resultInt2.ShouldBe(expectedResultInt + 1);
        resultString1.ShouldBe(expectedResultString);
        resultString2.ShouldBe($"Hi{expectedResultString}");
        resultId3?.ThreadId.ShouldBe(expectedResultId);
        resultId4?.ThreadId.ShouldBe(expectedResultId);
    }

    [Fact]
    public void On_Action_With_Multiple_Subscribers_And_DifferentEvents_And_Unsubscribe()
    {
        int resultInt1 = -1;
        int resultInt2 = -1;
        string resultString1 = "";
        string resultString2 = "Hi";

        var id = ThreadIdentifier.NewIdentifier();

        ThreadUpdated? resultId3 = new(id);
        ThreadUpdated? resultId4 = new(ThreadIdentifier.NewIdentifier());
        _sut.On<int>(i => resultInt1 = i, out var s1);
        _sut.On<string>(i => resultString1 = i, out var s2);
        _sut.On<ThreadUpdated>(i => resultId3 = i, out var s3);

        _sut.On<int>(i => resultInt2 = i + 1, out _);
        _sut.On<string>(i => resultString2 += i, out _);
        _sut.On<ThreadUpdated>(i => resultId4 = i, out _);

        var expectedResultInt = _fixture.Create<int>();
        var expectedResultString = _fixture.Create<string>();
        var expectedResultId = ThreadIdentifier.NewIdentifier();

        _sut.Off(s1);
        _sut.Off(s2);
        _sut.Off(s3);

        _sut.Send(expectedResultInt);
        _sut.Send(expectedResultString);
        _sut.Send(new ThreadUpdated(expectedResultId));
        resultInt1.ShouldBe(-1);
        resultInt2.ShouldBe(expectedResultInt + 1);
        resultString1.ShouldBe("");
        resultString2.ShouldBe($"Hi{expectedResultString}");
        resultId3?.ThreadId.ShouldBe(id);
        resultId4?.ThreadId.ShouldBe(expectedResultId);
    }

    [Fact]
    public void On_Action_With_Multiple_Subscribers_But_With_Unsubscribe()
    {
        int result1 = -1;
        int result2 = -1;
        int result3 = -1;
        _sut.On<int>(i => result1 = i + 1, out _);
        _sut.On<int>(i => result2 = i + 2, out var subscriptionId);
        _sut.On<int>(i => result3 = i + 3, out _);
        _sut.Off(subscriptionId);

        var expectedResult = _fixture.Create<int>();

        _sut.Send(expectedResult);
        result1.ShouldBe(expectedResult + 1);
        result2.ShouldBe(-1);
        result3.ShouldBe(expectedResult + 3);
    }

    [Fact]
    public async Task On_Concurrent_On()
    {
        var count = _fixture.Create<int>() + 100;

        var results = new int[count];
        var expectedResults = new int[count];
        var message = _fixture.Create<int>();

        var onTasks = new Task[count];
        for (var i = 0; i < results.Length; i++)
        {
            var index = i;
            results[i] = -1;
            expectedResults[i] = message + i;
            onTasks[i] = Task.Run(() => _sut.On<int>(msg => results[index] = msg + index, out _));
        }

        await Task.WhenAll(onTasks);

        _sut.Send(message);

        results.ShouldBeEquivalentTo(expectedResults);
    }


    [Fact]
    public async Task On_Concurrent_On_And_Off()
    {
        var count = _fixture.Create<int>() + 100;

        var results = new int[count];
        var expectedResults = new int[count];
        var message = _fixture.Create<int>();

        var onTasks = new Task[count];
        for (var i = 0; i < results.Length; i++)
        {
            var index = i;
            results[i] = -1;
            expectedResults[i] = (index % 2 == 0) ? -1 : message + i;
            onTasks[i] = Task.Run(async () =>
            {
                _sut.On<int>(msg => results[index] = msg + index, out var subscriptionId);
                if (index % 2 == 0)
                {
                    await Task.Run(() => _sut.Off(subscriptionId));
                }
            }, TestContext.Current.CancellationToken);
        }

        await Task.WhenAll(onTasks);

        _sut.Send(message);

        results.ShouldBeEquivalentTo(expectedResults);
    }

    [Fact]
    public async Task On_Concurrent_On_And_Off_And_Send()
    {
        var count = _fixture.Create<int>() + 100;

        var results = new int[count];
        var expectedResults = new int[count];
        var message = _fixture.Create<int>();

        var onTasks = new Task[count];
        var sendTasks = new Task[count];
        for (var i = 0; i < results.Length; i++)
        {
            sendTasks[i] = Task.Run(() => _sut.Send(message), TestContext.Current.CancellationToken);
            var index = i;
            results[i] = -1;
            expectedResults[i] = (index % 2 == 0) ? -1 : message + i;
            onTasks[i] = Task.Run(async () =>
            {
                _sut.On<int>(msg => results[index] = msg + index, out var subscriptionId);
                if (index % 2 == 0)
                {
                    await Task.Run(() => _sut.Off(subscriptionId));
                }
            }, TestContext.Current.CancellationToken);
        }

        await Task.WhenAll([.. onTasks, .. sendTasks]);
        Array.Fill(results, -1);

        _sut.Send(message);
        results.ShouldBeEquivalentTo(expectedResults);
    }


    [Fact]
    public void Send_On_No_Subscriber_Should_Do_Nothing()
    {
        _sut.Send(_fixture.Create<string>());
    }

    [Fact]
    public void Off_No_Subscriber_Should_Do_Nothing()
    {
        _sut.Off(new SubscriptionId<int>(_fixture.Create<string>()));

    }
}