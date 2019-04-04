namespace Nyar.Types;

/// <summary>
///     值类型枚举
/// </summary>
public enum ValueType
{
    /// <summary>
    ///     未知类型
    /// </summary>
    unknown,

    /// <summary>
    ///     空值
    /// </summary>
    @null,

    /// <summary>
    ///     布尔值
    /// </summary>
    @bool,

    /// <summary>
    ///     32位整数
    /// </summary>
    i32,

    /// <summary>
    ///     64位整数
    /// </summary>
    i64,

    /// <summary>
    ///     大整数
    /// </summary>
    big_int,

    /// <summary>
    ///     64位浮点
    /// </summary>
    f64,

    /// <summary>
    ///     对象引用
    /// </summary>
    @object,

    /// <summary>
    ///     字符串
    /// </summary>
    utf8,

    /// <summary>
    ///     闭包
    /// </summary>
    closure,

    /// <summary>
    ///     延续
    /// </summary>
    continuation,

    /// <summary>
    ///     效果
    /// </summary>
    effect,

    /// <summary>
    ///     见证表
    /// </summary>
    witness_table
}