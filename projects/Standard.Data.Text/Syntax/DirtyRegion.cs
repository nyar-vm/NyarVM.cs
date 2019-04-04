namespace Std.Data.Text.Syntax;

/// <summary>
///     脏区域，表示源码中被编辑影响的范围
/// </summary>
public readonly struct DirtyRegion : IEquatable<DirtyRegion>
{
    /// <summary>
    ///     脏区域的起始位置
    /// </summary>
    public int start { get; }

    /// <summary>
    ///     脏区域的长度
    /// </summary>
    public int length { get; }

    /// <summary>
    ///     脏区域的结束位置（不含）
    /// </summary>
    public int end => start + length;

    /// <summary>
    ///     创建指定范围的脏区域
    /// </summary>
    public DirtyRegion(int start, int length)
    {
        this.start = start;
        this.length = length;
    }

    /// <summary>
    ///     从 TextSpan 创建脏区域
    /// </summary>
    public static DirtyRegion from_span(TextSpan span)
    {
        return new DirtyRegion(span.start, span.length);
    }

    /// <summary>
    ///     从编辑操作计算脏区域
    /// </summary>
    public static DirtyRegion from_edit(Edit edit)
    {
        var start = edit.old_span.start;
        var length = System.Math.Max(edit.old_span.length, edit.new_text.Length);
        return new DirtyRegion(start, length);
    }

    /// <summary>
    ///     脏区域是否与指定节点重叠
    /// </summary>
    public bool overlaps_with(GreenNode node, int nodeOffset)
    {
        var nodeEnd = nodeOffset + node.width;
        return start < nodeEnd && nodeOffset < end;
    }

    /// <summary>
    ///     脏区域是否完全包含指定节点
    /// </summary>
    public bool contains(GreenNode node, int nodeOffset)
    {
        return start <= nodeOffset && end >= nodeOffset + node.width;
    }

    /// <summary>
    ///     转换为 TextSpan
    /// </summary>
    public TextSpan to_span()
    {
        return new TextSpan(start, length);
    }

    /// <summary>
    ///     如果重叠则合并两个脏区域，返回合并后的区域
    /// </summary>
    public DirtyRegion merge(DirtyRegion other)
    {
        var newStart = System.Math.Min(start, other.start);
        var newEnd = System.Math.Max(end, other.end);
        return new DirtyRegion(newStart, newEnd - newStart);
    }

    public bool Equals(DirtyRegion other)
    {
        return start == other.start && length == other.length;
    }

    public override bool Equals(object? obj)
    {
        return obj is DirtyRegion other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(start, length);
    }

    public static bool operator ==(DirtyRegion left, DirtyRegion right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(DirtyRegion left, DirtyRegion right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return $"[{start}..{end})";
    }
}