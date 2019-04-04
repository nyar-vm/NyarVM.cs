using Nyar.Types;

namespace Nyar.VM.NyarVM.Jit;

/// <summary>
///     栈上替换（OSR）入口点描述符。
///     它描述一个可以从解释模式切换到 JIT 编译模式的代码字节偏移。
/// </summary>
public sealed class OsrEntry
{
    /// <summary>
    ///     初始化 OsrEntry
    /// </summary>
    /// <param name="functionIndex">函数索引。</param>
    /// <param name="bytecodePc">代码字节偏移量（OSR 入口点）。</param>
    /// <param name="stackDepth">入口点处的操作数栈深度。</param>
    /// <param name="localCount">局部变量数量。</param>
    public OsrEntry(int functionIndex, int bytecodePc, int stackDepth, int localCount)
    {
        function_index = functionIndex;
        bytecode_pc = bytecodePc;
        stack_depth = stackDepth;
        local_count = localCount;
    }

    /// <summary>
    ///     函数索引
    /// </summary>
    public int function_index { get; }

    /// <summary>
    ///     代码字节偏移量（OSR 入口点）。
    ///     属性名沿用历史命名 `bytecode_pc`，但语义上表示代码区中的字节偏移。
    /// </summary>
    public int bytecode_pc { get; }

    /// <summary>
    ///     入口点处的操作数栈深度
    /// </summary>
    public int stack_depth { get; }

    /// <summary>
    ///     局部变量数量
    /// </summary>
    public int local_count { get; }

    /// <summary>
    ///     是否已编译
    /// </summary>
    public bool is_compiled => _compiled_delegate != null;

    /// <summary>
    ///     OSR 编译后的委托
    /// </summary>
    internal Func<Value[], Value[], Value>? _compiled_delegate { get; set; }
}
