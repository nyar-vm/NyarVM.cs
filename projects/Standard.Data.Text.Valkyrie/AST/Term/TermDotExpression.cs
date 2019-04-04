namespace Std.Data.Text.Valkyrie.AST.Term;

/// <summary>
///     成员访问表达式，如 <c>object.member</c>
/// </summary>
/// <para>示例：</para>
/// <code>
/// player.name             // Target = IdentifierNode("player"), MemberName = "name"
/// self.health             // Target = IdentifierNode("self"), MemberName = "health"
/// instance.method()       // 后续 TermCallExpression 引用此表达式作为 Callee
/// instance.a::b::method()       // 后续 TermCallExpression 引用此表达式作为 Callee
/// </code>
public sealed record TermDotExpression : TermNode
{
    public TermDotExpression(
        TermNode caller,
        QualifiedPathNode callee,
        CallBody? body = null,
        MemberAccessSeparatorKind separatorKind = MemberAccessSeparatorKind.dot)
    {
        this.caller = caller;
        this.callee = callee;
        call_body = body;
        separator_kind = separatorKind;
    }

    /// <summary>
    ///     访问的目标对象（左.之前的部分）
    /// </summary>
    public TermNode caller { get; init; }

    /// <summary>
    ///     调用形式
    /// </summary>
    public CallBody? call_body { get; init; }

    /// <summary>
    ///     成员访问分隔符。
    /// </summary>
    public MemberAccessSeparatorKind separator_kind { get; init; }

    /// <summary>
    ///     要访问的成员名称
    /// </summary>
    public QualifiedPathNode callee { get; init; }
}
