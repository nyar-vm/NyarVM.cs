namespace Std.Data.Text.Svg;

/// <summary>
///     SVG 文档，表示解析后的完整 SVG 内容
/// </summary>
public sealed class SvgDocument
{
    /// <summary>
    ///     SVG 根元素
    ///     。
    /// </summary>
    public SvgRootElement root { get; init; } = new();


    /// <summary>
    ///     文档宽度（从根元素获取）
    ///     。
    /// </summary>
    public float width => root.width;


    /// <summary>
    ///     文档高度（从根元素获取）
    ///     。
    /// </summary>
    public float height => root.height;


    /// <summary>
    ///     视图框（从根元素获取）
    ///     。
    /// </summary>
    public float[] view_box => root.view_box;


    /// <summary>
    ///     遍历所有元素（深度优先）
    ///     。
    /// </summary>
    public IEnumerable<SvgElement> enumerate_all()
    {
        return enumerate_descendants(root);
    }


    /// <summary>
    ///     遍历指定类型的所有元素
    ///     。
    /// </summary>
    public IEnumerable<T> enumerate_of_type<T>() where T : SvgElement
    {
        foreach (var element in enumerate_all())
            if (element is T typed)
                yield return typed;
    }


    /// <summary>
    ///     根据 ID 查找元素
    ///     。
    /// </summary>
    public SvgElement? find_by_id(string id)
    {
        foreach (var element in enumerate_all())
            if (element.id == id)
                return element;

        return null;
    }


    /// <summary>
    ///     获取所有可绘制元素（非容器、非定义元素）
    ///     。
    /// </summary>
    public IEnumerable<SvgElement> get_drawable_elements()
    {
        foreach (var element in enumerate_all())
        {
            if (element is SvgDefsElement) continue;

            if (!element.is_container) yield return element;
        }
    }

    private static IEnumerable<SvgElement> enumerate_descendants(SvgElement element)
    {
        yield return element;

        foreach (var child in element.children)
        foreach (var descendant in enumerate_descendants(child))
            yield return descendant;
    }
}