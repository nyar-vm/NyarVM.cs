namespace Std.Command.TypeConversion;

/// <summary>
///     float 类型转换器
/// </summary>
public sealed class Float32Converter : SimpleTypeConverter<float>
{
    /// <inheritdoc />
    public override string get_format_hint()
    {
        return "单精度浮点数（如 3.14）";
    }

    /// <inheritdoc />
    protected override bool try_parse(string input, out float result)
    {
        return float.TryParse(input, out result);
    }
}