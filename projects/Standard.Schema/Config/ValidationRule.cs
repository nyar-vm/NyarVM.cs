namespace Hermes.Config;

/// <summary>
///     验证规则 Base
/// </summary>
public abstract class ValidationRule
{
    /// <summary>
    ///     规则类型名
    /// </summary>
    public abstract string Kind { get; }
}

/// <summary>
///     值域范围验证
/// </summary>
public sealed class RangeValidation : ValidationRule
{
    public RangeValidation(double min, double max)
    {
        Min = min;
        Max = max;
    }

    /// <inheritdoc />
    public override string Kind => "range";

    /// <summary>
    ///     最小值
    /// </summary>
    public double Min { get; }

    /// <summary>
    ///     最大值
    /// </summary>
    public double Max { get; }
}

/// <summary>
///     枚举值验证
/// </summary>
public sealed class OneOfValidation : ValidationRule
{
    public OneOfValidation(IReadOnlyList<string> values)
    {
        Values = values;
    }

    /// <inheritdoc />
    public override string Kind => "one_of";

    /// <summary>
    ///     允许的值列表
    /// </summary>
    public IReadOnlyList<string> Values { get; }
}

/// <summary>
///     正则验证
/// </summary>
public sealed class RegexValidation : ValidationRule
{
    public RegexValidation(string pattern)
    {
        Pattern = pattern;
    }

    /// <inheritdoc />
    public override string Kind => "regex";

    /// <summary>
    ///     正则表达式模式
    /// </summary>
    public string Pattern { get; }
}

/// <summary>
///     长度验证
/// </summary>
public sealed class LengthValidation : ValidationRule
{
    public LengthValidation(int minLength, int maxLength)
    {
        MinLength = minLength;
        MaxLength = maxLength;
    }

    /// <inheritdoc />
    public override string Kind => "len";

    /// <summary>
    ///     最小长度
    /// </summary>
    public int MinLength { get; }

    /// <summary>
    ///     最大长度
    /// </summary>
    public int MaxLength { get; }
}

/// <summary>
///     自定义 micro 验证
/// </summary>
public sealed class CustomValidation : ValidationRule
{
    public CustomValidation(string microName)
    {
        MicroName = microName;
    }

    /// <inheritdoc />
    public override string Kind => "custom";

    /// <summary>
    ///     micro 函数名
    /// </summary>
    public string MicroName { get; }
}