namespace Std.Terminal.Controls;

/// <summary>
///     表格控件，支持列定义、排序、筛选、选中行、分页
/// </summary>
/// <typeparam name="T">数据行类型</typeparam>
public sealed class TableView<T> : View
{
    private readonly List<TableColumn> _columns = [];
    private int _current_page = 1;
    private string _filter_text = string.Empty;
    private IReadOnlyList<T> _items = [];
    private int _page_size = 10;
    private int _scroll_offset;
    private int _selected_row_index = -1;

    /// <summary>
    ///     列定义
    /// </summary>
    public IReadOnlyList<TableColumn> columns => _columns;

    /// <summary>
    ///     数据行集合
    /// </summary>
    public IReadOnlyList<T> items
    {
        get => _items;
        set
        {
            _items = value;
            _current_page = 1;
        }
    }

    /// <summary>
    ///     当前选中的行索引
    /// </summary>
    public int selected_row_index
    {
        get => _selected_row_index;
        set => _selected_row_index = System.Math.Clamp(value, -1, _filtered_items.Count - 1);
    }

    /// <summary>
    ///     当前选中的数据行
    /// </summary>
    public T? selected_row => _selected_row_index >= 0 && _selected_row_index < _filtered_items.Count
        ? _filtered_items[_selected_row_index]
        : default;

    /// <summary>
    ///     每页行数
    /// </summary>
    public int page_size
    {
        get => _page_size;
        set => _page_size = System.Math.Max(1, value);
    }

    /// <summary>
    ///     当前页码
    /// </summary>
    public int current_page
    {
        get => _current_page;
        set => _current_page = System.Math.Clamp(value, 1, total_pages);
    }

    /// <summary>
    ///     总页数
    /// </summary>
    public int total_pages => System.Math.Max(1, (_filtered_items.Count + _page_size - 1) / _page_size);

    /// <summary>
    ///     筛选文本（搜索所有可见列的值）
    /// </summary>
    public string filter_text
    {
        get => _filter_text;
        set
        {
            _filter_text = value;
            _current_page = 1;
            _selected_row_index = -1;
        }
    }

    /// <summary>
    ///     当前排序列
    /// </summary>
    public string? sort_column { get; private set; }

    /// <summary>
    ///     排序方向
    /// </summary>
    public SortDirection sort_dir { get; private set; } = SortDirection.none;

    private IReadOnlyList<T> _filtered_items
    {
        get
        {
            IEnumerable<T> result = _items;

            if (!string.IsNullOrEmpty(_filter_text))
                result = result.Where(item =>
                    _columns.Any(col =>
                    {
                        var value = get_property_value(item, col.property_name);
                        return value?.ToString()?.Contains(_filter_text, StringComparison.OrdinalIgnoreCase) == true;
                    }));

            if (sort_column != null && sort_dir != SortDirection.none)
                result = sort_dir == SortDirection.ascending
                    ? result.OrderBy(item => get_property_value(item, sort_column))
                    : result.OrderByDescending(item => get_property_value(item, sort_column));

            return [.. result];
        }
    }

    /// <summary>
    ///     行选中事件
    /// </summary>
    public event Action<TableView<T>, T?>? OnRowSelected;

    /// <summary>
    ///     行激活事件（双击或回车）
    /// </summary>
    public event Action<TableView<T>, T>? OnRowActivated;

    /// <summary>
    ///     添加列定义
    /// </summary>
    /// <param name="header">列标题</param>
    /// <param name="propertyName">属性名</param>
    /// <param name="width">列宽</param>
    public TableView<T> add_column(string header, string propertyName, int width = 0)
    {
        _columns.Add(new TableColumn
        {
            header = header,
            property_name = propertyName,
            width = width
        });
        return this;
    }

    /// <summary>
    ///     添加列定义（带格式化委托）
    /// </summary>
    /// <param name="header">列标题</param>
    /// <param name="propertyName">属性名</param>
    /// <param name="formatter">格式化委托</param>
    public TableView<T> add_column(string header, string propertyName, Func<object?, string> formatter)
    {
        _columns.Add(new TableColumn
        {
            header = header,
            property_name = propertyName,
            formatter = formatter
        });
        return this;
    }

    /// <summary>
    ///     按指定列排序并切换方向
    /// </summary>
    /// <param name="columnProperty">列属性名</param>
    public void sort_by(string columnProperty)
    {
        if (sort_column == columnProperty)
        {
            sort_dir = sort_dir == SortDirection.ascending
                ? SortDirection.descending
                : SortDirection.ascending;
        }
        else
        {
            sort_column = columnProperty;
            sort_dir = SortDirection.ascending;
        }

        _current_page = 1;
    }

    /// <summary>
    ///     下一页
    /// </summary>
    public void next_page()
    {
        if (_current_page < total_pages)
        {
            _current_page++;
            _selected_row_index = -1;
        }
    }

    /// <summary>
    ///     上一页
    /// </summary>
    public void previous_page()
    {
        if (_current_page > 1)
        {
            _current_page--;
            _selected_row_index = -1;
        }
    }

    /// <inheritdoc />
    public override void render(RenderContext ctx)
    {
        var filtered = _filtered_items;
        var totalWidth = calculate_total_width();

        render_header(ctx, totalWidth);
        render_rows(ctx, filtered, totalWidth);
        render_footer(ctx, totalWidth);
    }

    private void render_header(RenderContext ctx, int totalWidth)
    {
        var separator = new string('─', totalWidth + _columns.Count - 1);

        for (var i = 0; i < _columns.Count; i++)
        {
            var col = _columns[i];
            var sortIndicator = sort_column == col.property_name
                ? sort_dir == SortDirection.ascending ? " ↑" : " ↓"
                : string.Empty;

            var headerText = truncate(col.header + sortIndicator, get_column_width(col));
            var padded = align_text(headerText, get_column_width(col), col.alignment);
            ctx.draw_text(x + get_column_offset(i), y, padded);

            if (i < _columns.Count - 1) ctx.draw_text(x + get_column_offset(i) + get_column_width(col), y, "│");
        }

        ctx.draw_text(x, y + 1, separator);
    }

    private void render_rows(RenderContext ctx, IReadOnlyList<T> filtered, int totalWidth)
    {
        var rowsToShow = System.Math.Min(_page_size, height - 4);
        var startIndex = (_current_page - 1) * _page_size;
        var endIndex = System.Math.Min(startIndex + rowsToShow, filtered.Count);

        for (var rowIdx = startIndex; rowIdx < endIndex; rowIdx++)
        {
            var displayRow = rowIdx - startIndex;
            var row = filtered[rowIdx];
            var isSelected = rowIdx == _selected_row_index;

            if (isSelected) ctx.draw_text(x, y + 2 + displayRow, ">");

            for (var colIdx = 0; colIdx < _columns.Count; colIdx++)
            {
                var col = _columns[colIdx];
                var value = get_property_value(row, col.property_name);
                var text = col.formatter != null ? col.formatter(value) : value?.ToString() ?? string.Empty;
                var cellText = align_text(truncate(text, get_column_width(col) - 1), get_column_width(col),
                    col.alignment);
                ctx.draw_text(x + get_column_offset(colIdx) + 1, y + 2 + displayRow, cellText);
            }
        }
    }

    private void render_footer(RenderContext ctx, int totalWidth)
    {
        var footerY = y + height - 1;
        var info = $"第 {_current_page}/{total_pages} 页  共 {_items.Count} 行";
        if (!string.IsNullOrEmpty(_filter_text)) info += $"  筛选: \"{_filter_text}\" ({_filtered_items.Count} 匹配)";

        if (sort_column != null) info += $"  排序: {sort_column} {(sort_dir == SortDirection.ascending ? "↑" : "↓")}";

        ctx.draw_text(x, footerY, truncate(info, totalWidth));
    }

    private int calculate_total_width()
    {
        return _columns.Sum(c => get_column_width(c)) + _columns.Count - 1;
    }

    private int get_column_width(TableColumn col)
    {
        if (col.width > 0) return col.width;

        var availableWidth = width - 1 - (_columns.Count - 1);
        var autoColumns = _columns.Count(c => c.width <= 0);
        return System.Math.Max(col.min_width, availableWidth / System.Math.Max(1, autoColumns));
    }

    private int get_column_offset(int columnIndex)
    {
        var offset = 1;
        for (var i = 0; i < columnIndex; i++) offset += get_column_width(_columns[i]) + 1;

        return offset;
    }

    private static string truncate(string text, int maxWidth)
    {
        if (text.Length <= maxWidth) return text;

        return text[..(maxWidth - 1)] + "…";
    }

    private static string align_text(string text, int width, HorizontalAlignment alignment)
    {
        if (text.Length >= width) return text;

        return alignment switch
        {
            HorizontalAlignment.right => new string(' ', width - text.Length) + text,
            HorizontalAlignment.center => new string(' ', (width - text.Length) / 2) + text +
                                          new string(' ', (width - text.Length + 1) / 2),
            _ => text + new string(' ', width - text.Length)
        };
    }

    private static object? get_property_value(T obj, string propertyName)
    {
        var prop = typeof(T).GetProperty(propertyName);
        return prop?.GetValue(obj);
    }
}