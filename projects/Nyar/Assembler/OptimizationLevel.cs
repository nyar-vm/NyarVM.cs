namespace Nyar.Assembler;

/// <summary>
///     编译优化级别。
/// </summary>
public enum OptimizationLevel
{
    /// <summary> 无优化。</summary>
    none,

    /// <summary> 基础优化。</summary>
    basic,

    /// <summary> 进阶优化。</summary>
    aggressive
}