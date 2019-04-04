namespace Std.DataProcess;

/// <summary>
///     数据投影模式，决定类型在序列化时的结构映射方�?///
/// </summary>
public enum ProjectMode
{
    /// <summary>
    ///     映射模式，将字段按名称逐一映射到键值结�?    ///
    /// </summary>
    map = 0,

    /// <summary>
    ///     元组模式，将字段按位置顺序映射到有序结构
    /// </summary>
    tuple = 1
}