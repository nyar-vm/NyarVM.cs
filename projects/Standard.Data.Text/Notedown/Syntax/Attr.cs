namespace Std.Data.Text.Notedown.Syntax;

/// <summary>
///     通用属性：标识符 + CSS 类名列表 + 键值对属性
/// </summary>
public readonly record struct Attr
{
    /// <summary>
    ///     创建属性
    /// </summary>
    public Attr(string id, IReadOnlyList<string>? classes = null,
        IReadOnlyList<KeyValuePair<string, string>>? keyValues = null)
    {
        this.id = id;
        this.classes = classes ?? [];
        key_values = keyValues ?? [];
    }

    /// <summary>
    ///     元素标识符
    /// </summary>
    public string id { get; init; }

    /// <summary>
    ///     CSS 类名列表
    /// </summary>
    public IReadOnlyList<string> classes { get; init; }

    /// <summary>
    ///     键值对属性列表
    /// </summary>
    public IReadOnlyList<KeyValuePair<string, string>> key_values { get; init; }

    /// <summary>
    ///     空属性
    /// </summary>
    public static Attr empty => new(string.Empty);

    /// <summary>
    ///     判断是否为空
    /// </summary>
    public bool is_empty => id.Length == 0 && classes.Count == 0 && key_values.Count == 0;
}