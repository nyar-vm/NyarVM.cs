namespace Std.Command.TypeConversion;

/// <summary>
///     int 类型转换器
/// </summary>
public sealed class Int32Converter : SimpleTypeConverter<int>
{
    /// <inheritdoc />
    public override string get_format_hint()
    {
        return "整数（如 42）";
    }

    /// <inheritdoc />
    protected override bool try_parse(string input, out int result)
    {
        return int.TryParse(input, out result);
    }
}