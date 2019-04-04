namespace Std.Command.TypeConversion;

/// <summary>
///     string 类型转换器（直接透传）
/// </summary>
public sealed class StringConverter : ITypeConverter<string>
{
    /// <inheritdoc />
    public bool try_convert(string input, out string result)
    {
        result = input;
        return true;
    }

    /// <inheritdoc />
    public string get_format_hint()
    {
        return "文本";
    }
}