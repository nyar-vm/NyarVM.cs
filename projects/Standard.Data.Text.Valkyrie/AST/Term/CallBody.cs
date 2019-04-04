using Std.Data.Text.Valkyrie.AST.Statement;
using Std.Data.Text.Valkyrie.AST.Type;

namespace Std.Data.Text.Valkyrie.AST.Term;

/// <summary>
///     成员访问表达式，如 <c>object.member</c>
/// </summary>
/// <para>示例：</para>
/// <code>
/// f(args)
/// f(args) { block }
/// f { block }
/// f::<T>
///         (args)
///         f::
///         <T>
///             (args) { block }
///             x.f::<T>(args) { block }
/// </code>
public sealed record CallBody : ValkyrieNode
{
    /// <summary>
    ///     显式指定的泛型类型参数列表（<c>::&lt;T&gt;</c> 形式）
    /// </summary>
    public TypeArgumentList? type_arguments { get; init; }

    /// <summary>
    /// </summary>
    public TermArgumentList? term_arguments { get; init; }

    public FunctionBody? function_body { get; init; }
}