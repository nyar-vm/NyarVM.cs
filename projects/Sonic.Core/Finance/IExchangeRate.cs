namespace Core.Finance;

/// <summary>
///     汇率转换接口
/// </summary>
public interface IExchangeRate
{
    /// <summary>
    ///     将源金额转换为目标货币
    /// </summary>
    /// <param name="source">源金额</param>
    /// <param name="target">目标货币</param>
    /// <returns>转换后的金额数值</returns>
    decimal convert(IMoney source, ICurrency target);
}