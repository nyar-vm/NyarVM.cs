using System.Reflection;
using System.Text;

namespace Std.Command.Output;

/// <summary>
///     表格格式化器，使用 Unicode 框线字符渲染对象属性为表格
/// </summary>
/// <typeparam name="T">输出数据类型</typeparam>
public sealed class TableFormatter<T> : IOutputFormatter<T>
{
    /// <inheritdoc />
    public bool supports_format(OutputFormat format)
    {
        return format is OutputFormat.auto or OutputFormat.table;
    }

    /// <inheritdoc />
    public string format(T data)
    {
        if (data is null) return string.Empty;

        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        if (properties.Length == 0) return string.Empty;

        var headers = properties.Select(p => p.Name).ToArray();
        var values = properties.Select(p => format_value(p.GetValue(data))).ToArray();

        var columnWidths = new int[headers.Length];
        for (var i = 0; i < headers.Length; i++) columnWidths[i] = System.Math.Max(headers[i].Length, values[i].Length);

        var sb = new StringBuilder();

        sb.AppendLine(to_horizontal_line(columnWidths, '╭', '┬', '╮'));
        sb.AppendLine(to_data_row(columnWidths, headers, '│'));
        sb.AppendLine(to_horizontal_line(columnWidths, '├', '┼', '┤'));
        sb.AppendLine(to_data_row(columnWidths, values, '│'));
        sb.Append(to_horizontal_line(columnWidths, '╰', '┴', '╯'));

        return sb.ToString();
    }

    private static string to_horizontal_line(int[] widths, char left, char mid, char right)
    {
        var sb = new StringBuilder();
        sb.Append(left);

        for (var i = 0; i < widths.Length; i++)
        {
            sb.Append(new string('─', widths[i] + 2));
            if (i < widths.Length - 1) sb.Append(mid);
        }

        sb.Append(right);
        return sb.ToString();
    }

    private static string to_data_row(int[] widths, string[] cells, char separator)
    {
        var sb = new StringBuilder();
        sb.Append(separator);

        for (var i = 0; i < cells.Length; i++)
        {
            sb.Append(' ');
            sb.Append(cells[i].PadRight(widths[i]));
            sb.Append(' ');
            sb.Append(separator);
        }

        return sb.ToString();
    }

    private static string format_value(object? value)
    {
        if (value is null) return string.Empty;

        if (value is bool b) return b ? "是" : "否";

        if (value is DateTime dt) return dt.ToString("yyyy-MM-dd HH:mm:ss");

        return value.ToString() ?? string.Empty;
    }
}