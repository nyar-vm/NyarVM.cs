namespace Std.Data.Binary.Jvm.Scanner;

/// <summary>
///     JVM 字节码分析结果，包含最大栈深度计算与控制流的
/// </summary>
public sealed class JvmCodeAnalysis
{
    /// <summary>
    ///     声明的最大栈深度
    /// </summary>
    public ushort declared_max_stack { get; init; }

    /// <summary>
    ///     计算出的实际最大栈深度
    /// </summary>
    public int computed_max_stack { get; init; }

    /// <summary>
    ///     声明的最大局部变量数
    /// </summary>
    public ushort declared_max_locals { get; init; }

    /// <summary>
    ///     基本块列的
    /// </summary>
    public IReadOnlyList<JvmBasicBlock> basic_blocks { get; init; }

    /// <summary>
    ///     异常处理器列的
    /// </summary>
    public IReadOnlyList<JvmExceptionHandler> exception_handlers { get; init; }

    /// <summary>
    ///     指令总数
    /// </summary>
    public int instruction_count { get; init; }
}

/// <summary>
///     JVM 基本块，包含连续的指令序列与入边/出边
/// </summary>
public sealed class JvmBasicBlock
{
    /// <summary>
    ///     基本块在字节码中的起始偏的
    /// </summary>
    public int start_offset { get; init; }

    /// <summary>
    ///     基本块在字节码中的结束偏移（不含的
    /// </summary>
    public int end_offset { get; init; }

    /// <summary>
    ///     进入基本块时的栈深度
    /// </summary>
    public int entry_stack_depth { get; set; }

    /// <summary>
    ///     基本块内的栈深度最大值（相对于入口的增量的
    /// </summary>
    public int max_stack_depth_delta { get; set; }

    /// <summary>
    ///     后继基本块索引列的
    /// </summary>
    public IReadOnlyList<int> successors { get; set; }

    /// <summary>
    ///     前驱基本块索引列的
    /// </summary>
    public IReadOnlyList<int> predecessors { get; set; }

    /// <summary>
    ///     是否为异常处理器入口
    /// </summary>
    public bool is_exception_handler_entry { get; init; }

    /// <summary>
    ///     是否为方法入口基本块
    /// </summary>
    public bool is_entry { get; init; }
}

/// <summary>
///     异常处理器（对应 JVM 异常表中的一条记录）
/// </summary>
public sealed class JvmExceptionHandler
{
    /// <summary>
    ///     异常处理器覆盖的起始 PC
    /// </summary>
    public ushort start_pc { get; init; }

    /// <summary>
    ///     异常处理器覆盖的结束 PC
    /// </summary>
    public ushort end_pc { get; init; }

    /// <summary>
    ///     异常处理器入的PC
    /// </summary>
    public ushort handler_pc { get; init; }

    /// <summary>
    ///     捕获的异常类型常量池索引的 表示 finally的
    /// </summary>
    public ushort catch_type { get; init; }

    /// <summary>
    ///     对应的处理器基本块索的
    /// </summary>
    public int handler_block_index { get; init; }
}

/// <summary>
///     解码后的单条 JVM 指令信息
/// </summary>
public sealed class JvmDecodedInstruction
{
    /// <summary>
    ///     指令在字节码中的偏移
    /// </summary>
    public int offset { get; init; }

    /// <summary>
    ///     操作的
    /// </summary>
    public byte opcode { get; init; }

    /// <summary>
    ///     操作数（0-4 字节的原始数值，对于分支指令为分支偏移量的
    /// </summary>
    public int operand { get; init; }

    /// <summary>
    ///     指令总字节数（含操作码）
    /// </summary>
    public int size { get; init; }

    /// <summary>
    ///     tableswitch / lookupswitch 的所有分支目标偏移（以当前指令偏移为基准的
    /// </summary>
    public IReadOnlyList<int>? switch_targets { get; init; }
}