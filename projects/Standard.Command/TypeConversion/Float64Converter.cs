namespace Std.Command.TypeConversion;

/// <summary>
///     double 类型转换器
/// </summary>
public sealed class Float64Converter : SimpleTypeConverter<double>
{
    /// <inheritdoc />
    public override string get_format_hint()
    {
        return "浮点数（如 3.14）";
    }

    /// <inheritdoc />
    protected override bool try_parse(string input, out double result)
    {
        return double.TryParse(input, out result);
    }
}