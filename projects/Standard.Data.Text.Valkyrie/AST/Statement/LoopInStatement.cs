using Std.Data.Text.Syntax;
using Std.Data.Text.Valkyrie.AST.Pattern;

namespace Std.Data.Text.Valkyrie.AST.Statement;

/// <summary>
///     Loop 遍历语句（类似 foreach）
/// </summary>
public sealed record LoopInStatement : ValkyrieNode
{
    /// <summary>
    ///     无参构造函数
    /// </summary>
    public LoopInStatement()
    {
    }

    /// <summary>
    ///     兼容旧版仅变量名的构造函数
    /// </summary>
    public LoopInStatement(string? iteratorName, ValkyrieNode? iterable, FunctionBody body, TextSpan span)
        : this(
            iteratorName,
            iteratorName is null
                ? null
                : new PatternLiteralVariableNode { name = iteratorName },
            iterable,
            body,
            span)
    {
    }

    /// <summary>
    ///     完整构造函数
    /// </summary>
    public LoopInStatement(string? iteratorName, PatternNode? iteratorPattern, ValkyrieNode? iterable, FunctionBody body,
        TextSpan span)
    {
        iterator_name = iteratorName;
        iterator_pattern = iteratorPattern;
        this.iterable = iterable;
        this.body = body;
        this.span = span;
    }

    /// <summary>
    ///     迭代变量名称
    /// </summary>
    public string? iterator_name { get; init; }

    /// <summary>
    ///     迭代变量模式
    /// </summary>
    public PatternNode? iterator_pattern { get; init; }

    /// <summary>
    ///     被遍历的可迭代对象
    /// </summary>
    public ValkyrieNode? iterable { get; init; }

    /// <summary>
    ///     循环体
    /// </summary>
    public FunctionBody body { get; init; } = new();
}
