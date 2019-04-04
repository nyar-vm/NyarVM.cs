namespace Std.Data.Text.Brainfuck;


/// <summary>

///     Brainfuck 命令基类


/// </summary>
public abstract record BfCommand;


/// <summary>

///     指针移动（> 或 <）


/// </summary>

/// <param name="Offset">移动偏移量，正数向右，负数向左。</param>
public sealed record BfCmdMove(int Offset) : BfCommand;


/// <summary>

///     单元格增减（+ 或 -）


/// </summary>

/// <param name="Value">增减值，正数为增加，负数为减少。</param>
public sealed record BfCmdAdd(int Value) : BfCommand;


/// <summary>

///     输出（.）


/// </summary>
public sealed record BfCmdOutput : BfCommand;


/// <summary>

///     输入（,）


/// </summary>
public sealed record BfCmdInput : BfCommand;


/// <summary>

///     循环（[...]）


/// </summary>

/// <param name="Body">循环体内的命令列表。</param>
public sealed record BfCmdLoop(IReadOnlyList<BfCommand> Body) : BfCommand;
