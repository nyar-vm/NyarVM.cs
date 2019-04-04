using Core.Finance;

namespace Std.Finance;

/// <summary>
///     汇率转换器，实现 IExchangeRate 接口，提供货币间的汇率转换
/// </summary>
public sealed class ExchangeRate : IExchangeRate
{
    /// <summary>
    ///     汇率表，键为 "源货币代码-目标货币代码"
    /// </summary>
    private readonly Dictionary<string, decimal> _rates = new();

    /// <summary>
    ///     将源金额转换为目标货币
    /// </summary>
    /// <param name="source">源金额</param>
    /// <param name="target">目标货币</param>
    /// <returns>转换后的金额数值</returns>
    public decimal convert(IMoney source, ICurrency target)
    {
        if (source.currency.code == target.code) return source.amount;

        var key = $"{source.currency.code}-{target.code}";
        if (_rates.TryGetValue(key, out var rate)) return source.amount * rate;

        throw new InvalidOperationException($"未找到从 {source.currency.code} 到 {target.code} 的汇率");
    }

    /// <summary>
    ///     设置汇率
    /// </summary>
    /// <param name="from">源货币代码</param>
    /// <param name="to">目标货币代码</param>
    /// <param name="rate">汇率</param>
    public void set_rate(string from, string to, decimal rate)
    {
        _rates[$"{from}-{to}"] = rate;
    }
}