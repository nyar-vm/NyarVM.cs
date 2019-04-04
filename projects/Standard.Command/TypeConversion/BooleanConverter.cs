namespace Std.Command.TypeConversion;

/// <summary>
///     bool 类型转换器
/// </summary>
public sealed class BooleanConverter : SimpleTypeConverter<bool>
{
    /// <inheritdoc />
    public override string get_format_hint()
    {
        return "true 或 false";
    }

    /// <inheritdoc />
    protected override bool try_parse(string input, out bool result)
    {
        return bool.TryParse(input, out result);
    }
}