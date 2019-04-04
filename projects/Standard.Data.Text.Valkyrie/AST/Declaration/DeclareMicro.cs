using Std.Data.Text.Valkyrie.AST.Statement;
using Std.Data.Text.Valkyrie.AST.Type;

namespace Std.Data.Text.Valkyrie.AST.Declaration;

/// <summary>
///     函数声明节点
/// </summary>
/// <para>示例：</para>
/// <code>
/// micro add(x: i32, y: i32) -> i32 {
///     return x + y;
/// }
/// </code>
/// <para>支持泛型：</para>
/// <code>
/// micro identity&lt;T&gt;(value: T) -> T {
///     return value;
/// }
/// </code>
/// <para>支持属性：</para>
/// <code>
/// [import("std.math", "min")]
/// micro min() { ... }
/// </code>
public sealed record DeclareMicro : ValkyrieNode, IDeclarationNode
{
    /// <summary>
    ///     参数列表
    /// </summary>
    public IReadOnlyList<TermParameterList> parameters { get; init; } = [];

    /// <summary>
    ///     返回值类型注解，为 <c>null</c> 时表示使用 <c>auto</c> 推导
    /// </summary>
    public TypeNode? return_type { get; init; }

    /// <summary>
    ///     函数体代码块，外部函数/抽象函数可为 <c>null</c>
    /// </summary>
    public FunctionBody? body { get; init; }

    /// <summary>
    ///     泛型类型参数列表
    /// </summary>
    public IReadOnlyList<TypeParameterList> type_parameters { get; init; } = [];

    /// <summary>
    ///     泛型约束列表（<c>where</c> 子句）
    /// </summary>
    public IReadOnlyList<GenericConstraint> generic_constraints { get; init; } = [];

    /// <summary>
    ///     函数名称
    /// </summary>
    public IdentifierNode? name { get; init; } = new();

    /// <summary>
    ///     注解信息
    /// </summary>
    public Annotations annotations { get; init; } = new();
}