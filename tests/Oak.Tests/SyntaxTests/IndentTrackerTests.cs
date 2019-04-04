using Oak.Utilities;

namespace Oak.Core.Tests.SyntaxTests;

public class IndentTrackerTests
{
    private const int TimeoutMs = 5000;

    private static T RunWithTimeout<T>(Func<T> action, int timeoutMs = TimeoutMs)
    {
        T result = default!;
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                result = action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });
        thread.Start();
        if (!thread.Join(timeoutMs))
        {
            thread.Interrupt();
            thread.Join(100);
            throw new TimeoutException($"操作在 {timeoutMs}ms 内未完成，可能存在死循环");
        }
        if (exception is not null)
        {
            throw exception;
        }

        return result;
    }

    [Fact]
    public void IndentTracker_InitialIndentIsZero()
    {
        var tracker = new IndentTracker();
        Assert.Equal(0, tracker.CurrentIndent);
    }

    [Fact]
    public void IndentTracker_IndentIncrease()
    {
        var events = RunWithTimeout(() => new IndentTracker().ProcessLine(4));
        Assert.Single(events);
        Assert.Equal(IndentEvent.Indent, events[0]);
    }

    [Fact]
    public void IndentTracker_IndentDecrease()
    {
        var tracker = new IndentTracker();
        RunWithTimeout(() => tracker.ProcessLine(4));
        var events = RunWithTimeout(() => tracker.ProcessLine(0));
        Assert.Single(events);
        Assert.Equal(IndentEvent.Dedent, events[0]);
        Assert.Equal(0, tracker.CurrentIndent);
    }

    [Fact]
    public void IndentTracker_SameIndent_NoEvent()
    {
        var events = RunWithTimeout(() => new IndentTracker().ProcessLine(0));
        Assert.Empty(events);
    }

    [Fact]
    public void IndentTracker_MultipleIndents()
    {
        var tracker = new IndentTracker();
        RunWithTimeout(() => { tracker.ProcessLine(4); return true; });
        RunWithTimeout(() => { tracker.ProcessLine(8); return true; });
        Assert.Equal(8, tracker.CurrentIndent);
    }

    [Fact]
    public void IndentTracker_CloseProducesRemainingDedents()
    {
        var tracker = new IndentTracker();
        RunWithTimeout(() => { tracker.ProcessLine(4); return true; });
        RunWithTimeout(() => { tracker.ProcessLine(8); return true; });
        var events = RunWithTimeout(() => tracker.Close());
        Assert.Equal(2, events.Count);
        Assert.All(events, e => Assert.Equal(IndentEvent.Dedent, e));
    }

    [Fact]
    public void IndentTracker_Reset()
    {
        var tracker = new IndentTracker();
        RunWithTimeout(() => { tracker.ProcessLine(4); return true; });
        RunWithTimeout(() => { tracker.Reset(); return true; });
        Assert.Equal(0, tracker.CurrentIndent);
    }
}
