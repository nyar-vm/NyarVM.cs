using System.Globalization;

namespace Std.Data.Text.Yaml;

/// <summary>
///     YAML 数字值
/// </summary>
public sealed class YamlNumber : YamlValue
{
    public YamlNumber(double value)
    {
        this.value = value;
    }

    public double value { get; }

    public override YamlValueType ValueType => YamlValueType.number;


    /// <summary>
    ///     尝试获取整数值
    /// </summary>
    public bool try_get_int(out int value)
    {
        if (this.value is >= int.MinValue and <= int.MaxValue && this.value == (int)this.value)
        {
            value = (int)this.value;
            return true;
        }

        value = default;
        return false;
    }


    /// <summary>
    ///     尝试获取长整数值
    /// </summary>
    public bool try_get_long(out long value)
    {
        if (this.value is >= long.MinValue and <= long.MaxValue && this.value == (long)this.value)
        {
            value = (long)this.value;
            return true;
        }

        value = default;
        return false;
    }

    public override string ToString()
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }
}