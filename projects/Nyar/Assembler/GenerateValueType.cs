namespace Nyar.Assembler;

/// <summary>
///     代码生成中间表示的值类型枚举，替代 Operand.type 的字符串表示。
///     各后端通过此枚举进行类型映射，编译器保证穷尽性。
/// </summary>
public enum GenerateValueType : byte
{
    /// <summary>
    ///     无返回值
    /// </summary>
    @void,

    /// <summary>
    ///     语言级 `Unit` 值。
    ///     当前 AOT 后端统一使用整数零作为最小运行时表示。
    /// </summary>
    unit,

    /// <summary>
    ///     布尔类型
    /// </summary>
    @bool,


    /// <summary>
    ///     8 位有符号整数
    /// </summary>
    i8,


    /// <summary>
    ///     16 位有符号整数
    /// </summary>
    i16,


    /// <summary>
    ///     32 位有符号整数
    /// </summary>
    i32,


    /// <summary>
    ///     64 位有符号整数
    /// </summary>
    i64,

    /// <summary>
    ///     128 位有符号整数
    /// </summary>
    i128,

    /// <summary>
    ///     32 位浮点数
    /// </summary>
    f32,

    /// <summary>
    ///     64 位浮点数
    /// </summary>
    f64,

    /// <summary>
    ///     Unicode 字符类型
    /// </summary>
    @char,

    /// <summary>
    ///     UTF8 字符串类型
    /// </summary>
    utf8,

    /// <summary>
    ///     UTF16 字符串类型
    /// </summary>
    utf16,

    /// <summary>
    ///     对象类型
    /// </summary>
    @object,


    /// <summary>
    ///     任意类型（动态类型）
    /// </summary>
    any,

    /// <summary>
    ///     语言级 `Unit` 值。
    ///     当前 AOT 后端统一使用整数零作为最小运行时表示。
    /// </summary>
    @null,

    /// <summary>
    ///     函数引用类型
    /// </summary>
    function_ref,

    /// <summary>
    ///     外部引用类型
    /// </summary>
    external_ref,

    /// <summary>
    ///     128 位 SIMD 向量类型
    /// </summary>
    v128
}