namespace Core.Finance;

/// <summary>
///     金额接口
/// </summary>
public interface IMoney
{
    /// <summary>
    ///     金额数值
    /// </summary>
    decimal amount { get; }

    /// <summary>
    ///     货币类型
    /// </summary>
    ICurrency currency { get; }
}