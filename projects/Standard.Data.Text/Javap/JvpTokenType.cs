namespace Std.Data.Text.Javap;

/// <summary>
///     Javap Token 类型
/// </summary>
public enum JvpTokenType
{
    /// <summary>
    ///     文件结束
    /// </summary>
    eof,


    /// <summary>
    ///     访问修饰符（public, private, static 等）
    /// </summary>
    access_modifier,


    /// <summary>
    ///     类型关键字（class, interface, enum 等）
    /// </summary>
    type_keyword,


    /// <summary>
    ///     JVM 操作码（小写下划线格式：aload_0, invokevirtual 等）
    /// </summary>
    opcode,


    /// <summary>
    ///     标识符（类名、方法名、字段名）
    /// </summary>
    identifier,


    /// <summary>
    ///     类型名（int, void, java.lang.String 等）
    /// </summary>
    type_name,


    /// <summary>
    ///     数字（偏移量、操作数）
    /// </summary>
    number,


    /// <summary>
    ///     常量池引用（#1, #42 等）
    /// </summary>
    constant_pool_ref,


    /// <summary>
    ///     注释（// 开头）
    /// </summary>
    comment,


    /// <summary>
    ///     标点符号（{, }, ( ), ;, :, ., ,）
    /// </summary>
    punctuation,


    /// <summary>
    ///     "Compiled" 头部关键字
    /// </summary>
    header_keyword,


    /// <summary>
    ///     "Code:" 段标记
    /// </summary>
    section_marker
}