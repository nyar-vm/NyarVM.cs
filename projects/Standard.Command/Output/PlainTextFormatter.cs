using System.Reflection;
using System.Text;

namespace Std.Command.Output;

/// <summary>
///     纯文本格式化器，以 "键: 值" 行格式输出对象属性
/// </summary>
/// <typeparam name="T">输出数据类型</typeparam>
public sealed class PlainTextFormatter<T> : IOutputFormatter<T>
{
    /// <inheritdoc />
    public bool supports_format(OutputFormat format)
    {
        return format is OutputFormat.auto or OutputFormat.plain;
    }

    /// <inheritdoc />
    public string format(T data)
    {
        if (data is null) return string.Empty;

        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        if (properties.Length == 0) return data.ToString() ?? string.Empty;

        var sb = new StringBuilder();

        foreach (var prop in properties)
        {
            var value = prop.GetValue(data);
            sb.AppendLine($"{prop.Name}: {format_value(value)}");
        }

        return sb.ToString().TrimEnd();
    }

    private static string format_value(object? value)
    {
        if (value is null) return string.Empty;

        if (value is bool b) return b ? "是" : "否";

        if (value is DateTime dt) return dt.ToString("yyyy-MM-dd HH:mm:ss");

        return value.ToString() ?? string.Empty;
    }
}