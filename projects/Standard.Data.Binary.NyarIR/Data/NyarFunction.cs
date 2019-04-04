namespace Std.Data.Binary.NyarIR.Data;

/// <summary>
///     Nyar 函数元数据。
///     这里只描述函数在代码区中的范围，不直接承载解码后的指令对象。
/// </summary>
public sealed class NyarFunction
{
    public NyarFunction(string name, int arity, int localCount, int codeLength)
    {
        this.name = name;
        this.arity = arity;
        local_count = localCount;
        code_length = codeLength;
    }

    public NyarFunction(string name, int arity, int localCount, int codeOffset, int codeLength)
    {
        this.name = name;
        this.arity = arity;
        local_count = localCount;
        code_offset = codeOffset;
        code_length = codeLength;
    }

    /// <summary>
    ///     函数名称。
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     参数数量。
    /// </summary>
    public int arity { get; init; }

    /// <summary>
    ///     局部变量数量。
    /// </summary>
    public int local_count { get; init; }

    /// <summary>
    ///     代码区起始字节偏移。
    ///     指向扁平代码字节流中的起始位置，不是“第几条指令”。
    /// </summary>
    public int code_offset { get; init; }

    /// <summary>
    ///     代码区字节长度。
    ///     表示编码后的字节跨度，不是指令条数。
    /// </summary>
    public int code_length { get; init; }
}