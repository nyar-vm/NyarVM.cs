namespace Std.Data.Text.Msil;

/// <summary>
///     ILASM Token 类型
/// </summary>
public enum MsilTokenType
{
    /// <summary>
    ///     文件结束
    /// </summary>
    eof,


    /// <summary>
    ///     指令关键字（.assembly, .class, .method 等）
    /// </summary>
    directive,


    /// <summary>
    ///     MSIL 操作码（ldarg.0, add, call 等，点分格式）
    /// </summary>
    opcode,


    /// <summary>
    ///     标识符
    /// </summary>
    identifier,


    /// <summary>
    ///     类型引用（int32, string, [mscorlib]System.Console 等）
    /// </summary>
    type_reference,


    /// <summary>
    ///     数字
    /// </summary>
    number,


    /// <summary>
    ///     字符串字面量
    /// </summary>
    string_literal,


    /// <summary>
    ///     IL 偏移标签（IL_0001:）
    /// </summary>
    il_label,


    /// <summary>
    ///     注释（// 开头）
    /// </summary>
    comment,


    /// <summary>
    ///     访问修饰符（public, private, static 等）
    /// </summary>
    access_modifier,


    /// <summary>
    ///     标点符号
    /// </summary>
    punctuation
}