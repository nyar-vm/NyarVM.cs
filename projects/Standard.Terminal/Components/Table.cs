using System.Text;

namespace Std.Terminal.Components;

/// <summary>
///     表格渲染组件，支持列宽计算和边框样式。
/// </summary>
public sealed class Table : IRenderable
{
    private readonly List<TableColumn> _columns;
    private readonly List<string[]> _rows;

    /// <summary>
    ///     初始化 <see cref="Table" /> 的新实例。
    /// </summary>
    public Table()
    {
        _columns = [];
        _rows = [];
        desired_width = 0;
    }

    /// <summary>
    ///     获取或设置是否显示边框。
    /// </summary>
    public bool show_border { get; set; } = true;

    /// <summary>
    ///     获取布局的期望宽度。
    /// </summary>
    public int desired_width { get; private set; }

    /// <summary>
    ///     获取布局的期望高度。
    /// </summary>
    public int desired_height => _rows.Count * 2 + 1;

    /// <summary>
    ///     在指定区域内渲染此表格。
    /// </summary>
    /// <param name="renderer">终端渲染器。</param>
    /// <param name="region">渲染区域。</param>
    public void render(AnsiTerminalRenderer renderer, Rectangle region)
    {
        if (_columns.Count == 0) return;

        var widths = calculate_column_widths(region.width);
        var row = 0;

        if (show_border)
        {
            renderer.set_cursor_position(region.x, region.y + row);
            renderer.write(AnsiString.plain(build_horizontal_border(widths, '+', '+', '+')).ansi_sequence);
            row++;
        }

        renderer.set_cursor_position(region.x, region.y + row);
        renderer.write(AnsiString.plain(build_row(widths, [.. _columns.Select(c => c.header)])).ansi_sequence);
        row++;

        if (show_border)
        {
            renderer.set_cursor_position(region.x, region.y + row);
            renderer.write(AnsiString.plain(build_horizontal_border(widths, '+', '+', '+')).ansi_sequence);
            row++;
        }

        foreach (var dataRow in _rows)
        {
            if (row >= region.height) break;

            renderer.set_cursor_position(region.x, region.y + row);
            renderer.write(AnsiString.plain(build_row(widths, dataRow)).ansi_sequence);
            row++;
        }

        if (show_border && row < region.height)
        {
            renderer.set_cursor_position(region.x, region.y + row);
            renderer.write(AnsiString.plain(build_horizontal_border(widths, '+', '+', '+')).ansi_sequence);
        }
    }

    /// <summary>
    ///     添加列定义。
    /// </summary>
    /// <param name="header">列标题。</param>
    /// <param name="width">列宽，`0` 表示自动计算。</param>
    public void add_column(string header, int width = 0)
    {
        _columns.Add(new TableColumn(header, width));
        recalculate_width();
    }

    /// <summary>
    ///     添加一行数据。
    /// </summary>
    /// <param name="cells">单元格值数组。</param>
    public void add_row(params string[] cells)
    {
        _rows.Add(cells);
        recalculate_width();
    }

    private void recalculate_width()
    {
        desired_width = 0;
        for (var i = 0; i < _columns.Count; i++)
        {
            var maxW = _columns[i].header.Length;
            foreach (var row in _rows)
                if (i < row.Length && row[i].Length > maxW)
                    maxW = row[i].Length;

            if (_columns[i].width > 0)
                maxW = _columns[i].width;
            else
                _columns[i] = new TableColumn(_columns[i].header, maxW);

            desired_width += maxW + 3;
        }

        desired_width += 1;
    }

    private int[] calculate_column_widths(int availableWidth)
    {
        return [.. _columns.Select(c => c.width > 0 ? c.width : c.header.Length)];
    }

    private static string build_row(int[] widths, string[] cells)
    {
        var sb = new StringBuilder();
        sb.Append('|');
        for (var i = 0; i < widths.Length; i++)
        {
            var cell = i < cells.Length ? cells[i] : "";
            sb.Append(' ');
            sb.Append(cell.PadRight(widths[i]));
            sb.Append(' ');
            sb.Append('|');
        }

        return sb.ToString();
    }

    private static string build_horizontal_border(int[] widths, char left, char middle, char right)
    {
        var sb = new StringBuilder();
        sb.Append(left);
        for (var i = 0; i < widths.Length; i++)
        {
            sb.Append(new string('─', widths[i] + 2));
            sb.Append(i < widths.Length - 1 ? middle : right);
        }

        return sb.ToString();
    }

    private readonly struct TableColumn
    {
        public string header { get; }
        public int width { get; }

        public TableColumn(string header, int width)
        {
            this.header = header;
            this.width = width;
        }
    }
}