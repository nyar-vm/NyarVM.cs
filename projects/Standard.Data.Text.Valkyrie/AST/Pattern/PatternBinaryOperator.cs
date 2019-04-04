namespace Std.Data.Text.Valkyrie.AST.Pattern;

public enum PatternBinaryOperator
{
    add,
    subtract,
    multiply,
    divide,

    /// <summary>
    ///     | a
    /// </summary>
    fake_or,

    /// <summary>
    ///     a..=b 闭区间范围模式
    /// </summary>
    range_inclusive
}