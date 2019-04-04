namespace Std.Data.Binary.Clr;

/// <summary>
///     CLR 数组类型引用，表示元素类型的数组
/// </summary>
public sealed class ClrArrayType : ClrType
{
    /// <summary>
    ///     创建数组类型引用
    /// </summary>
    /// <param name="elementType">元素类型。</param>
    /// <param name="rank">数组维度，默认为 1。</param>
    public ClrArrayType(ClrType elementType, int rank = 1)
    {
        element_type = elementType;
        this.rank = rank;
    }

    /// <summary>
    ///     数组元素类型
    /// </summary>
    public ClrType element_type { get; }

    /// <summary>
    ///     数组维度（秩的
    /// </summary>
    public int rank { get; }
}