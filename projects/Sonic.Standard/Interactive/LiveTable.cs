using System.Text;
using Sonic.Terminal;

namespace Sonic.Interactive;

/// <summary>
/// 动态表格，支持实时更新行列、排序、筛选、自动列宽、对齐和样式
/// </summary>
public sealed class LiveTable
{
    private readonly List<TableColumn> _columns = [];
    private readonly List<TableRow> _rows = [];
    private string _filter_query = string.Empty;
    private int _filter_column_index = -1;
    private int _sort_column_index = -1;
    private bool _sort_ascending = true;
    private bool _show_row_count = true;
    private int _max_table_width = 0;
    private int _last_rendered_lines;

    /// <summary>
    /// 获取所有行
    /// </summary>
    public IReadOnlyList<TableRow> rows => _rows;

    /// <summary>
    /// 获取所有列
    /// </summary>
    public IReadOnlyList<TableColumn> columns => _columns;

    /// <summary>
    /// 获取筛选后的行数
    /// </summary>
    public int filtered_row_count => get_processed_rows().Count();

    /// <summary>
    /// 添加列
    /// </summary>
    /// <param name="title">列标题</param>
    /// <param name="width">列宽，0 或负数表示自动列宽</param>
    /// <param name="alignment">对齐方式</param>
    public LiveTable add_column(string title, int width = 0, ColumnAlignment alignment = ColumnAlignment.left)
    {
        _columns.Add(new TableColumn(title, width, alignment));
        return this;
    }

    /// <summary>
    /// 添加行
    /// </summary>
    /// <param name="cells">单元格内容</param>
    /// <returns>表格行</returns>
    public TableRow add_row(string[] cells)
    {
        var row = new TableRow(cells);
        _rows.Add(row);
        return row;
    }

    /// <summary>
    /// 在指定位置插入行
    /// </summary>
    /// <param name="index">插入位置</param>
    /// <param name="cells">单元格内容</param>
    /// <returns>表格行</returns>
    public TableRow insert_row(int index, string[] cells)
    {
        var row = new TableRow(cells);
        _rows.Insert(index, row);
        return row;
    }

    /// <summary>
    /// 移除指定行
    /// </summary>
    /// <param name="row">要移除的行</param>
    /// <returns>是否成功移除</returns>
    public bool remove_row(TableRow row)
    {
        return _rows.Remove(row);
    }

    /// <summary>
    /// 移除指定索引的行
    /// </summary>
    /// <param name="index">行索引</param>
    public void remove_row_at(int index)
    {
        if (index >= 0 && index < _rows.Count)
        {
            _rows.RemoveAt(index);
        }
    }

    /// <summary>
    /// 替换指定行的全部单元格
    /// </summary>
    /// <param name="index">行索引</param>
    /// <param name="cells">新的单元格内容</param>
    public void replace_row(int index, string[] cells)
    {
        if (index >= 0 && index < _rows.Count)
        {
            _rows[index] = new TableRow(cells);
        }
    }

    /// <summary>
    /// 按指定列排序
    /// </summary>
    /// <param name="columnIndex">列索引</param>
    /// <param name="ascending">是否升序</param>
    public LiveTable sort_by(int columnIndex, bool ascending = true)
    {
        _sort_column_index = columnIndex;
        _sort_ascending = ascending;
        return this;
    }

    /// <summary>
    /// 按指定列筛选
    /// </summary>
    /// <param name="columnIndex">列索引</param>
    /// <param name="query">筛选查询字符串，大小写不敏感</param>
    public LiveTable filter_by(int columnIndex, string query)
    {
        _filter_column_index = columnIndex;
        _filter_query = query;
        return this;
    }

    /// <summary>
    /// 清除筛选条件
    /// </summary>
    public LiveTable clear_filter()
    {
        _filter_column_index = -1;
        _filter_query = string.Empty;
        return this;
    }

    /// <summary>
    /// 清除排序条件
    /// </summary>
    public LiveTable clear_sort()
    {
        _sort_column_index = -1;
        return this;
    }

    /// <summary>
    /// 设置是否显示行数统计
    /// </summary>
    /// <param name="show">是否显示</param>
    public LiveTable show_row_count(bool show = true)
    {
        _show_row_count = show;
        return this;
    }

    /// <summary>
    /// 设置表格最大宽度（0 表示不限制）
    /// </summary>
    /// <param name="maxWidth">最大宽度（字符数）</param>
    public LiveTable with_max_width(int maxWidth)
    {
        _max_table_width = maxWidth;
        return this;
    }

    /// <summary>
    /// 计算自动列宽
    /// </summary>
    private int[] calculate_column_widths()
    {
        var widths = new int[_columns.Count];

        for (var i = 0; i < _columns.Count; i++)
        {
            if (_columns[i].width > 0)
            {
                widths[i] = _columns[i].width;
            }
            else
            {
                var maxLen = _columns[i].title.Length;
                foreach (var row in _rows)
                {
                    if (i < row.cells.Count)
                    {
                        var cellLen = (row.cells[i] ?? string.Empty).Length;
                        if (cellLen > maxLen)
                        {
                            maxLen = cellLen;
                        }
                    }
                }

                widths[i] = System.Math.Max(3, maxLen + 2);
            }
        }

        if (_max_table_width > 0)
        {
            var totalWidth = widths.Sum() + widths.Length + 1;
            while (totalWidth > _max_table_width && widths.Max() > 4)
            {
                var maxIdx = Array.IndexOf(widths, widths.Max());
                widths[maxIdx]--;
                totalWidth--;
            }
        }

        return widths;
    }

    /// <summary>
    /// 获取经过排序和筛选的行集合
    /// </summary>
    private IEnumerable<TableRow> get_processed_rows()
    {
        IEnumerable<TableRow> rows = _rows;

        if (_filter_column_index >= 0 && _filter_column_index < _columns.Count && !string.IsNullOrEmpty(_filter_query))
        {
            var col = _filter_column_index;
            var query = _filter_query.ToLowerInvariant();
            rows = rows.Where(r =>
                col < r.cells.Count &&
                (r.cells[col] ?? string.Empty).ToLowerInvariant().Contains(query));
        }

        if (_sort_column_index >= 0 && _sort_column_index < _columns.Count)
        {
            var col = _sort_column_index;
            var asc = _sort_ascending;
            var sorted = rows.ToList();
            sorted.Sort((a, b) =>
            {
                var aVal = col < a.cells.Count ? (a.cells[col] ?? string.Empty) : string.Empty;
                var bVal = col < b.cells.Count ? (b.cells[col] ?? string.Empty) : string.Empty;
                var cmp = string.Compare(aVal, bVal, StringComparison.OrdinalIgnoreCase);
                return asc ? cmp : -cmp;
            });
            rows = sorted;
        }

        return rows;
    }

    /// <summary>
    /// 渲染表格到字符串
    /// </summary>
    /// <returns>渲染后的表格文本</returns>
    public string render()
    {
        if (_columns.Count == 0)
        {
            return string.Empty;
        }

        var widths = calculate_column_widths();
        var processedRows = get_processed_rows().ToList();
        var sb = new StringBuilder();

        sb.AppendLine(build_top_border(widths));
        sb.AppendLine(build_header_row(widths));
        sb.AppendLine(build_separator(widths));

        foreach (var row in processedRows)
        {
            sb.AppendLine(build_data_row(row, widths));
        }

        sb.AppendLine(build_bottom_border(widths));

        if (_show_row_count)
        {
            var totalRows = _rows.Count;
            var visibleRows = processedRows.Count;
            if (_filter_column_index >= 0 && !string.IsNullOrEmpty(_filter_query))
            {
                sb.AppendLine($"  显示 {visibleRows}/{totalRows} 行");
            }
            else
            {
                sb.AppendLine($"  共 {totalRows} 行");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// 渲染带样式的表格到字符串
    /// </summary>
    /// <returns>带 ANSI 样式的表格文本</returns>
    public string render_styled()
    {
        if (_columns.Count == 0)
        {
            return string.Empty;
        }

        var widths = calculate_column_widths();
        var processedRows = get_processed_rows().ToList();
        var sb = new StringBuilder();

        var borderColor = AnsiStyling.bold(AnsiStyling.cyan(""));
        var resetCode = "\u001b[0m";

        sb.AppendLine($"{borderColor}{build_top_border(widths)}{resetCode}");
        sb.AppendLine($"{borderColor}{build_header_row(widths, styled: true)}{resetCode}");
        sb.AppendLine($"{borderColor}{build_separator(widths)}{resetCode}");

        foreach (var row in processedRows)
        {
            sb.AppendLine(build_data_row(row, widths));
        }

        sb.AppendLine($"{borderColor}{build_bottom_border(widths)}{resetCode}");

        if (_show_row_count)
        {
            var totalRows = _rows.Count;
            var visibleRows = processedRows.Count;
            if (_filter_column_index >= 0 && !string.IsNullOrEmpty(_filter_query))
            {
                sb.AppendLine(AnsiStyling.dim($"  显示 {visibleRows}/{totalRows} 行"));
            }
            else
            {
                sb.AppendLine(AnsiStyling.dim($"  共 {totalRows} 行"));
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// 渲染表格到控制台
    /// </summary>
    public void render_to_console()
    {
        var rendered = TerminalCapability.color_level != TerminalColorLevel.none
            ? render_styled()
            : render();
        if (rendered.Length > 0)
        {
            System.Console.Write(rendered);
        }
    }

    /// <summary>
    /// 实时刷新表格（清除上次渲染并重新输出）
    /// </summary>
    public void refresh()
    {
        if (_last_rendered_lines > 0)
        {
            for (var i = 0; i < _last_rendered_lines; i++)
            {
                System.Console.Write("\u001b[A");
                System.Console.Write(AnsiStyling.clear_line());
            }
        }

        render_to_console();
        _last_rendered_lines = count_rendered_lines();
    }

    /// <summary>
    /// 计算渲染后的行数
    /// </summary>
    private int count_rendered_lines()
    {
        if (_columns.Count == 0)
        {
            return 0;
        }

        var processedRows = get_processed_rows().Count();
        var lines = 4 + processedRows;
        if (_show_row_count)
        {
            lines++;
        }

        return lines;
    }

    private static string build_top_border(int[] widths)
    {
        var sb = new StringBuilder();
        sb.Append('┌');
        for (var i = 0; i < widths.Length; i++)
        {
            if (i > 0)
            {
                sb.Append('┬');
            }

            sb.Append(new string('─', widths[i]));
        }

        sb.Append('┐');
        return sb.ToString();
    }

    private string build_header_row(int[] widths, bool styled = false)
    {
        var sb = new StringBuilder();
        sb.Append('│');
        for (var i = 0; i < widths.Length; i++)
        {
            var title = i < _columns.Count ? _columns[i].title : string.Empty;
            var alignment = i < _columns.Count ? _columns[i].alignment : ColumnAlignment.left;
            var padded = alignment switch
            {
                ColumnAlignment.center => pad_center(title, widths[i]),
                ColumnAlignment.right => pad_right(title, widths[i]),
                _ => pad_left(title, widths[i])
            };

            if (styled)
            {
                sb.Append(AnsiStyling.bold(padded));
            }
            else
            {
                sb.Append(padded);
            }

            sb.Append('│');
        }

        return sb.ToString();
    }

    private static string build_separator(int[] widths)
    {
        var sb = new StringBuilder();
        sb.Append('├');
        for (var i = 0; i < widths.Length; i++)
        {
            if (i > 0)
            {
                sb.Append('┼');
            }

            sb.Append(new string('─', widths[i]));
        }

        sb.Append('┤');
        return sb.ToString();
    }

    private string build_data_row(TableRow row, int[] widths)
    {
        var sb = new StringBuilder();
        sb.Append('│');
        for (var i = 0; i < widths.Length; i++)
        {
            var cell = i < row.cells.Count ? (row.cells[i] ?? string.Empty) : string.Empty;
            var alignment = i < _columns.Count ? _columns[i].alignment : ColumnAlignment.left;
            var padded = alignment switch
            {
                ColumnAlignment.center => pad_center(cell, widths[i]),
                ColumnAlignment.right => pad_right(cell, widths[i]),
                _ => pad_left(cell, widths[i])
            };

            sb.Append(padded);
            sb.Append('│');
        }

        return sb.ToString();
    }

    private static string build_bottom_border(int[] widths)
    {
        var sb = new StringBuilder();
        sb.Append('└');
        for (var i = 0; i < widths.Length; i++)
        {
            if (i > 0)
            {
                sb.Append('┴');
            }

            sb.Append(new string('─', widths[i]));
        }

        sb.Append('┘');
        return sb.ToString();
    }

    private static string pad_center(string text, int width)
    {
        if (text.Length >= width)
        {
            return truncate(text, width);
        }

        var leftPad = (width - text.Length) / 2;
        var rightPad = width - text.Length - leftPad;
        return new string(' ', leftPad) + text + new string(' ', rightPad);
    }

    private static string pad_left(string text, int width)
    {
        if (text.Length >= width)
        {
            return truncate(text, width);
        }

        return " " + text.PadRight(width - 1);
    }

    private static string pad_right(string text, int width)
    {
        if (text.Length >= width)
        {
            return truncate(text, width);
        }

        return text.PadLeft(width - 1) + " ";
    }

    private static string truncate(string text, int width)
    {
        if (width <= 1)
        {
            return new string(' ', width);
        }

        return text[..(width - 2)] + "… ";
    }

    /// <summary>
    /// 清除所有行
    /// </summary>
    public void clear()
    {
        _rows.Clear();
    }
}