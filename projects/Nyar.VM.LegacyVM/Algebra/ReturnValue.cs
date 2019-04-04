namespace Nyar.VM.LegacyVM.Algebra;

/// <summary>
///     返回值哨兵，用于在块语句中标记函数返回。
/// </summary>
public sealed class ReturnValue
{
    /// <summary>
    ///     返回的值
    /// </summary>
    public object Value { get; }

    /// <summary>
    ///     创建返回值哨兵
    /// </summary>
    /// <param name="value">返回的值</param>
    public ReturnValue(object value)
    {
        Value = value;
    }
}