namespace Std.Database.Core;

/// <summary>
///     序列号，用于 MVCC 快照和 WAL 日志排序
/// </summary>
internal readonly record struct SequenceNumber(ulong value)
{
    /// <summary>
    ///     零值
    /// </summary>
    public static readonly SequenceNumber zero = new(0);

    /// <summary>
    ///     无效值
    /// </summary>
    public static readonly SequenceNumber invalid = new(ulong.MaxValue);

    /// <summary>
    ///     下一个序列号
    /// </summary>
    public SequenceNumber next => new(value + 1);

    /// <inheritdoc />
    public override string ToString()
    {
        return value.ToString();
    }
}