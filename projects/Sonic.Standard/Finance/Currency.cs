using Core.Finance;

namespace Std.Finance;

/// <summary>
///     货币，实现 ICurrency 接口，表示一种货币
/// </summary>
public sealed class Currency : ICurrency
{
    /// <summary>
    ///     初始化货币
    /// </summary>
    /// <param name="code">货币代码，如 USD、CNY</param>
    /// <param name="decimalDigits">小数位数</param>
    public Currency(string code, int decimalDigits = 2)
    {
        this.code = code;
        decimal_digits = decimalDigits;
    }

    /// <summary>
    ///     人民币
    /// </summary>
    public static Currency cny => new("CNY");

    /// <summary>
    ///     美元
    /// </summary>
    public static Currency usd => new("USD");

    /// <summary>
    ///     欧元
    /// </summary>
    public static Currency eur => new("EUR");

    /// <summary>
    ///     日元
    /// </summary>
    public static Currency jpy => new("JPY", 0);

    /// <summary>
    ///     货币代码
    /// </summary>
    public string code { get; }

    /// <summary>
    ///     小数位数
    /// </summary>
    public int decimal_digits { get; }
}