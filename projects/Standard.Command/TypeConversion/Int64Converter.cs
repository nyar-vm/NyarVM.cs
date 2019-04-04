namespace Std.Command.TypeConversion;

/// <summary>
///     long 类型转换器
/// </summary>
public sealed class Int64Converter : SimpleTypeConverter<long>
{
    /// <inheritdoc />
    public override string get_format_hint()
    {
        return "长整数（如 9223372036854775807）";
    }

    /// <inheritdoc />
    protected override bool try_parse(string input, out long result)
    {
        return long.TryParse(input, out result);
    }
}