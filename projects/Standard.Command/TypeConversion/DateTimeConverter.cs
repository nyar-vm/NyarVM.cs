namespace Std.Command.TypeConversion;

/// <summary>
///     DateTime 类型转换器
/// </summary>
public sealed class DateTimeConverter : SimpleTypeConverter<DateTime>
{
    /// <inheritdoc />
    public override string get_format_hint()
    {
        return "日期时间（如 2026-06-01 或 2026-06-01T12:00:00）";
    }

    /// <inheritdoc />
    protected override bool try_parse(string input, out DateTime result)
    {
        return DateTime.TryParse(input, out result);
    }
}