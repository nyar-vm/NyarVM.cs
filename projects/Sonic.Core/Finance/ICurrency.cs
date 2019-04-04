namespace Core.Finance;

/// <summary>
///     货币接口
/// </summary>
public interface ICurrency
{
    /// <summary>
    ///     货币代码
    /// </summary>
    string code { get; }

    /// <summary>
    ///     小数位数
    /// </summary>
    int decimal_digits { get; }
}