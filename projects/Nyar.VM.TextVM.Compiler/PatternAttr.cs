using System.Collections.Immutable;

namespace Nyar.VM.TextVM.Compiler;

/// <summary>
/// 模式复杂度等级。
/// </summary>
public enum Complexity
{
    /// <summary>
    /// 纯字面量模式。
    /// </summary>
    Literal,

    /// <summary>
    /// 正则语言模式（含 |、*、+、?、&amp;、!，无反向引用）。
    /// </summary>
    Regular,

    /// <summary>
    /// 上下文无关/更高级别（含反向引用）。
    /// </summary>
    ContextFree,
}

/// <summary>
/// DFA 状态数量估计。
/// </summary>
public enum DfaEstimate
{
    /// <summary>
    /// 小型 DFA，少于 100 个状态。
    /// </summary>
    Small,

    /// <summary>
    /// 中型 DFA，少于 1000 个状态。
    /// </summary>
    Medium,

    /// <summary>
    /// 大型 DFA，超过 1000 个状态。
    /// </summary>
    Large,
}

/// <summary>
/// 静态分析的聚合结果，包含复杂度、前缀、编码分析及警告信息。
/// </summary>
public readonly struct PatternAttr
{
    /// <summary>
    /// 复杂度等级。
    /// </summary>
    public Complexity Complexity { get; init; }

    /// <summary>
    /// 字面量前缀列表（按编码编码为字节数组）。
    /// </summary>
    public ImmutableArray<Byte[]> PrefixLiterals { get; init; }

    /// <summary>
    /// 是否仅包含 ASCII 字符（&lt; 128）。
    /// </summary>
    public Boolean IsAsciiOnly { get; init; }

    /// <summary>
    /// 是否仅包含 BMP 字符（U+0000-U+FFFF）。
    /// </summary>
    public Boolean IsBmpOnly { get; init; }

    /// <summary>
    /// 模式的最小匹配字节长度。
    /// </summary>
    public Int32 MinByteLen { get; init; }

    /// <summary>
    /// DFA 状态数量估计。
    /// </summary>
    public DfaEstimate StateEstimate { get; init; }

    /// <summary>
    /// 编译警告集合。
    /// </summary>
    public ImmutableArray<CompileWarning> Warnings { get; init; }
}
