using Std.Data.Text.Valkyrie.AST.Declaration;
using Std.Data.Text.Valkyrie.AST.Statement;
using Std.Data.Text.Valkyrie.AST.Type;

namespace Std.Data.Text.Valkyrie.AST.Term;

/// <summary>
///     Lambda 表达式，匿名函数定义
/// </summary>
/// <para>示例：</para>
/// <code>
/// micro() { ... }
/// micro(x, y) -> usize { x + y }
/// </code>
public sealed record AnonymousMicro : TermNode
{
    /// <summary>
    ///     参数列表（可省略类型标注）
    /// </summary>
    public IReadOnlyList<TermParameterList> parameters { get; init; } = [];

    /// <summary>
    ///     Lambda 体，可以是单个表达式或 <see cref="FunctionBody" />
    /// </summary>
    public FunctionBody? body { get; init; } = null;

    /// <summary>
    ///     返回值类型注解
    /// </summary>
    public TypeNode? return_type { get; init; }
}