namespace Std.DataProcess.Serialize;

/// <summary>
///     Serde 中性字面量树的节点分类。
/// </summary>
public enum SerdeValueType
{
    /// <summary>
    ///     空值
    /// </summary>
    @null,

    /// <summary>
    ///     布尔值
    /// </summary>
    boolean,

    /// <summary>
    ///     整数（无范围限制，使用字符串存储）
    /// </summary>
    integer,

    /// <summary>
    ///     小数（无范围限制，使用字符串存储）
    /// </summary>
    @decimal,

    /// <summary>
    ///     字符串
    /// </summary>
    @string,

    /// <summary>
    ///     数组
    /// </summary>
    array,

    /// <summary>
    ///     对象（键值对集合）
    /// </summary>
    @object
}
