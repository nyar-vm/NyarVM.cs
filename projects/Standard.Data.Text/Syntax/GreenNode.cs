using System.Text;

namespace Std.Data.Text.Syntax;

/// <summary>
///     Green 语法树节点抽象基类
/// </summary>
public abstract class GreenNode
{
    /// <summary>
    ///     节点类型标识
    /// </summary>
    public abstract NodeKind kind { get; }

    /// <summary>
    ///     节点在源码中的总字符跨度
    /// </summary>
    public abstract int width { get; }

    /// <summary>
    ///     子节点数量
    /// </summary>
    public abstract int child_count { get; }

    /// <summary>
    ///     是否为叶子节点
    /// </summary>
    public bool is_leaf => child_count == 0;

    /// <summary>
    ///     总文本长度（用于预分配 StringBuilder）
    /// </summary>
    public virtual int full_width => width;

    /// <summary>
    ///     获取所有子节点（惰性枚举）
    /// </summary>
    public IEnumerable<GreenNode> children
    {
        get
        {
            for (var i = 0; i < child_count; i++)
            {
                var child = get_child(i);
                if (child is not null) yield return child;
            }
        }
    }

    /// <summary>
    ///     获取指定索引的子节点
    /// </summary>
    public abstract GreenNode? get_child(int index);

    /// <summary>
    ///     将节点文本写入 TextWriter（虚拟方法，遍历所有子节点写入）
    /// </summary>
    public virtual void write_to(TextWriter writer)
    {
        for (var i = 0; i < child_count; i++)
        {
            var child = get_child(i);
            child?.write_to(writer);
        }
    }

    /// <summary>
    ///     批量写入 StringBuilder
    /// </summary>
    public void write_to(StringBuilder sb)
    {
        write_to_string_builder(sb);
    }

    /// <summary>
    ///     将整个子树文本写入 StringBuilder（预分配容量优化）
    /// </summary>
    protected virtual void write_to_string_builder(StringBuilder sb)
    {
        sb.EnsureCapacity(sb.Length + width);
        for (var i = 0; i < child_count; i++)
        {
            var child = get_child(i);
            if (child is GreenLeafNode leaf)
            {
                if (leaf.text is not null) sb.Append(leaf.text);
            }
            else
            {
                child?.write_to_string_builder(sb);
            }
        }
    }

    /// <summary>
    ///     获取整棵子树的文本内容
    /// </summary>
    public override string ToString()
    {
        var sb = new StringBuilder(width);
        write_to_string_builder(sb);
        return sb.ToString();
    }

    /// <summary>
    ///     判断从指定偏移量开始的节点是否与目标范围重叠
    /// </summary>
    public bool overlaps_with(TextSpan span, int offset)
    {
        var nodeEnd = offset + width;
        return span.start < nodeEnd && offset < span.end;
    }

    /// <summary>
    ///     判断从指定偏移量开始的节点是否被目标范围完全包含
    /// </summary>
    public bool is_contained_by(TextSpan span, int offset)
    {
        return span.start <= offset && span.end >= offset + width;
    }
}