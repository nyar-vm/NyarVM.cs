namespace Std.Data.Text.Testing;

public abstract class TestBase
{
    protected virtual int _timeout_ms => 1000;

    protected T execute_with_timeout<T>(Func<T> action, string operationName)
    {
        T? result = default;
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
        })
        {
            IsBackground = true,
            Priority = ThreadPriority.BelowNormal
        };

        thread.Start();

        if (!thread.Join(_timeout_ms)) throw new TimeoutException($"{operationName} 在 {_timeout_ms}ms 内未完成，可能存在死循环");

        if (exception is not null) throw exception;

        return result!;
    }

    protected void execute_with_timeout(Action action, string operationName)
    {
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        })
        {
            IsBackground = true,
            Priority = ThreadPriority.BelowNormal
        };

        thread.Start();

        if (!thread.Join(_timeout_ms)) throw new TimeoutException($"{operationName} 在 {_timeout_ms}ms 内未完成，可能存在死循环");

        if (exception is not null) throw exception;
    }
}