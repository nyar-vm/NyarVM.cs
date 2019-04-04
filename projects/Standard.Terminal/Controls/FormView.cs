namespace Std.Terminal.Controls;

/// <summary>
///     表单控件，支持多字段输入、标签对齐和验证
/// </summary>
public sealed class FormView : View
{
    private readonly List<FormField> _fields = [];
    private int _focused_field_index;
    private int _label_width = 12;

    /// <summary>
    ///     表单字段列表
    /// </summary>
    public IReadOnlyList<FormField> fields => _fields;

    /// <summary>
    ///     标签列宽
    /// </summary>
    public int label_width
    {
        get => _label_width;
        set => _label_width = System.Math.Max(4, value);
    }

    /// <summary>
    ///     字段间间距（行数）
    /// </summary>
    public int field_spacing { get; set; } = 1;

    /// <summary>
    ///     字段值变化事件
    /// </summary>
    public event Action<FormView, FormField>? OnFieldChanged;

    /// <summary>
    ///     表单提交事件
    /// </summary>
    public event Action<FormView>? OnSubmit;

    /// <summary>
    ///     添加文本输入字段
    /// </summary>
    /// <param name="label">标签</param>
    /// <param name="name">字段名</param>
    /// <param name="initialValue">初始值</param>
    public FormView add_text_field(string label, string name, string initialValue = "")
    {
        var textBox = new TextBox
        {
            text = initialValue,
            width = width - _label_width - 3
        };

        _fields.Add(new FormField
        {
            label = label,
            name = name,
            input = textBox
        });

        return this;
    }

    /// <summary>
    ///     添加下拉选择字段
    /// </summary>
    /// <param name="label">标签</param>
    /// <param name="name">字段名</param>
    /// <param name="items">选项列表</param>
    public FormView add_dropdown_field(string label, string name, IEnumerable<DropdownItem> items)
    {
        var dropdown = new Dropdown();
        foreach (var item in items) dropdown.add_item(item.text, item.value);

        dropdown.width = width - _label_width - 3;

        _fields.Add(new FormField
        {
            label = label,
            name = name,
            input = dropdown
        });

        return this;
    }

    /// <summary>
    ///     添加带验证的文本字段
    /// </summary>
    /// <param name="label">标签</param>
    /// <param name="name">字段名</param>
    /// <param name="validator">验证委托，返回 null 表示通过，否则返回错误信息</param>
    public FormView add_text_field(string label, string name, Func<string?, string?> validator)
    {
        add_text_field(label, name);
        _fields[^1].validator = validator;
        return this;
    }

    /// <summary>
    ///     获取字段值
    /// </summary>
    /// <param name="name">字段名</param>
    /// <returns>字段值字符串</returns>
    public string? get_field_value(string name)
    {
        var field = _fields.Find(f => f.name == name);
        if (field?.input is TextBox tb) return tb.text;

        if (field?.input is Dropdown dd) return dd.selected_item?.text;

        return null;
    }

    /// <summary>
    ///     获取所有字段的键值对
    /// </summary>
    public Dictionary<string, string?> get_all_values()
    {
        var result = new Dictionary<string, string?>();
        foreach (var field in _fields) result[field.name] = get_field_value(field.name);

        return result;
    }

    /// <summary>
    ///     验证所有字段
    /// </summary>
    /// <returns>是否全部通过验证</returns>
    public bool validate()
    {
        var allValid = true;
        foreach (var field in _fields)
            if (field.validator != null)
            {
                var value = get_field_value(field.name);
                var error = field.validator(value);
                field.error_message = error;
                if (error != null) allValid = false;
            }

        return allValid;
    }

    /// <inheritdoc />
    public override void render(RenderContext ctx)
    {
        for (var i = 0; i < _fields.Count; i++)
        {
            var field = _fields[i];
            var rowY = y + i * (1 + field_spacing);

            var paddedLabel = (field.label + ":").PadRight(_label_width);
            ctx.draw_text(x, rowY, paddedLabel);

            if (field.input != null)
            {
                var inputX = x + _label_width + 1;
                field.input.x = inputX;
                field.input.y = rowY;
                field.input.render(ctx);
            }

            if (!string.IsNullOrEmpty(field.error_message))
                ctx.draw_text(x + _label_width + 1, rowY + 1, field.error_message);
        }
    }
}