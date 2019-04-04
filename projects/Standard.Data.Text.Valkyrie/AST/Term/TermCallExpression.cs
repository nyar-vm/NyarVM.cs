namespace Std.Data.Text.Valkyrie.AST.Term;

/// <summary>
///     函数调用表达式，如 <c>func(arg1, arg2)</c>
/// </summary>
/// <para>支持泛型函数调用：</para>
/// <code>
/// print("hello");                      // Callee = IdentifierNode("print"), Arguments = [LiteralExpr("hello")]
/// add::&lt;int&gt;(a, b);              // Callee = IdentifierNode("add"), TypeArguments = [TypeAnnotation("int")]
/// obj.method(x, y);                    // Callee = MemberAccessExpr { Target = IdentifierNode("obj"), MemberName = "method" }
/// </code>
public sealed record TermCallExpression : TermNode
{
    public TermCallExpression(TermNode caller, CallBody callBody)
    {
        this.caller = caller;
        call_body = callBody;
    }

    /// <summary>
    ///     被调用的表达式（函数名或成员访问表达式）
    /// </summary>
    public TermNode caller { get; init; }

    /// <summary>
    ///     调用形式
    /// </summary>
    public CallBody call_body { get; init; }
}