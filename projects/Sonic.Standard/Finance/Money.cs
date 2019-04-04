using Core.Finance;

namespace Std.Finance;

/// <summary>
///     金额，实现 IMoney 接口，表示带有货币类型的金额
/// </summary>
public readonly struct Money : IMoney
{
    /// <summary>
    ///     金额数值
    /// </summary>
    public decimal amount { get; }

    /// <summary>
    ///     货币类型
    /// </summary>
    public ICurrency currency { get; }

    /// <summary>
    ///     初始化金额
    /// </summary>
    /// <param name="amount">金额数值</param>
    /// <param name="currency">货币类型</param>
    public Money(decimal amount, ICurrency currency)
    {
        this.amount = amount;
        this.currency = currency;
    }

    /// <summary>
    ///     加法运算
    /// </summary>
    /// <param name="left">左操作数</param>
    /// <param name="right">右操作数</param>
    /// <returns>相加后的金额</returns>
    public static Money operator +(Money left, Money right)
    {
        return new Money(left.amount + right.amount, left.currency);
    }

    /// <summary>
    ///     减法运算
    /// </summary>
    /// <param name="left">左操作数</param>
    /// <param name="right">右操作数</param>
    /// <returns>金额差</returns>
    public static Money operator -(Money left, Money right)
    {
        return new Money(left.amount - right.amount, left.currency);
    }

    /// <summary>
    ///     乘法运算
    /// </summary>
    /// <param name="left">金额</param>
    /// <param name="multiplier">乘数</param>
    /// <returns>金额积</returns>
    public static Money operator *(Money left, decimal multiplier)
    {
        return new Money(left.amount * multiplier, left.currency);
    }
}