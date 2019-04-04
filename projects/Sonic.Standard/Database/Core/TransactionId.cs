namespace Std.Database.Core;

/// <summary>
///     事务唯一标识
/// </summary>
internal readonly record struct TransactionId(ulong value)
{
    private static ulong _counter;

    /// <summary>
    ///     最小值
    /// </summary>
    public static readonly TransactionId min = new(0);

    /// <summary>
    ///     创建新的事务 ID
    /// </summary>
    /// <returns>新的事务 ID</returns>
    public static TransactionId @new()
    {
        return new TransactionId(Interlocked.Increment(ref _counter));
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return value.ToString();
    }
}