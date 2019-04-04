namespace Oak.Wat;

/// <summary>
///     WAT Token 类型
/// </summary>
public enum WatTokenType
{
    /// <summary>
    ///     文件结束
    /// </summary>
    Eof,


    /// <summary>
    ///     关键字（module, func, import, export, memory, table, global, data, type, param, result, local, block, loop, if, then,
    ///     else, end 等）
    /// </summary>
    Keyword,


    /// <summary>
    ///     WASM 操作码助记符（i32.add, local.get, call, br 等）
    /// </summary>
    Opcode,


    /// <summary>
    ///     标识符（$name 形式）
    /// </summary>
    Identifier,


    /// <summary>
    ///     数字字面量
    /// </summary>
    Number,


    /// <summary>
    ///     字符串字面量
    /// </summary>
    StringLiteral,


    /// <summary>
    ///     注释（;; 开头）
    /// </summary>
    Comment,


    /// <summary>
    ///     标点符号（( ), ;）
    /// </summary>
    Punctuation,


    /// <summary>
    ///     值类型关键字（i32, i64, f32, f64, funcref, externref）
    /// </summary>
    ValueType
}