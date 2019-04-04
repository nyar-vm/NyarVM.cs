namespace Std.Data.Text.Jasmin;

/// <summary>
///     Jasmin Token 类型
/// </summary>
public enum JmTokenType
{
    /// <summary>
    ///     文件结束
    /// </summary>
    eof,


    /// <summary>
    ///     指令关键字（.class, .super, .method 等）
    /// </summary>
    directive,


    /// <summary>
    ///     JVM 操作码（aload, invokevirtual 等）
    /// </summary>
    opcode,


    /// <summary>
    ///     标识符
    /// </summary>
    identifier,


    /// <summary>
    ///     类型描述符（Ljava/lang/String;、I、[I 等）
    /// </summary>
    descriptor,


    /// <summary>
    ///     数字字面量
    /// </summary>
    number,


    /// <summary>
    ///     字符串字面量
    /// </summary>
    string_literal,


    /// <summary>
    ///     标签（如 Label:）
    /// </summary>
    label,


    /// <summary>
    ///     注释（; 开头）
    /// </summary>
    comment,


    /// <summary>
    ///     访问修饰符（public, private, static, final 等）
    /// </summary>
    access_modifier,


    /// <summary>
    ///     冒号
    /// </summary>
    colon,


    /// <summary>
    ///     等号
    /// </summary>
    equals
}