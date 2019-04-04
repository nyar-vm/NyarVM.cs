using System;

namespace Core.Finance;

/// <summary>
///     标记表示货币的属性或字段
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class CurrencyAttribute : Attribute
{
    /// <summary>
    ///     初始化货币属性
    /// </summary>
    /// <param name="code">货币代码，如 USD、CNY</param>
    public CurrencyAttribute(string code)
    {
        this.code = code;
    }

    /// <summary>
    ///     货币代码
    /// </summary>
    public string code { get; }
}