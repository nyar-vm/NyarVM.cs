namespace Std.Data.Binary.Clr;

/// <summary>
///     原始类型种类枚举
/// </summary>
public enum PrimitiveKind
{
    /// <summary>
    ///     无返回值类的
    /// </summary>
    @void,

    /// <summary>
    ///     布尔类型
    /// </summary>
    boolean,

    /// <summary>
    ///     32 位有符号整数类型
    /// </summary>
    int32,

    /// <summary>
    ///     64 位有符号整数类型
    /// </summary>
    int64,

    /// <summary>
    ///     32 位浮点数类型
    /// </summary>
    float32,

    /// <summary>
    ///     64 位浮点数类型
    /// </summary>
    float64,

    /// <summary>
    ///     字符串类的
    /// </summary>
    @string,

    /// <summary>
    ///     对象类型
    /// </summary>
    @object
}