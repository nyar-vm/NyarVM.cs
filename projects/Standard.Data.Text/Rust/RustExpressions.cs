using Std.Data.Text.Syntax;

namespace Std.Data.Text.Rust;

#region 表达式


/// <summary>

///     二元运算表达式


/// </summary>
public sealed record RustBinaryOp(RustAstNode left, string @operator, RustAstNode right, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);


/// <summary>

///     一元运算表达式


/// </summary>
public sealed record RustUnaryOp(string @operator, RustAstNode operand, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);


/// <summary>

///     字面量表达式


/// </summary>
public sealed record RustLiteral(string kind, string value, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);


/// <summary>

///     标识符表达式


/// </summary>
public sealed record RustIdentifier(string name, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);


/// <summary>

///     函数调用表达式


/// </summary>
public sealed record RustCall(RustAstNode function, IReadOnlyList<RustAstNode> arguments, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);


/// <summary>

///     方法调用表达式


/// </summary>
public sealed record RustMethodCall(
    RustAstNode receiver,
    string method,
    IReadOnlyList<RustAstNode> arguments,
    TextSpan Span = default(TextSpan))
    : RustAstNode(Span);


/// <summary>

///     字段访问表达式


/// </summary>
public sealed record RustFieldAccess(RustAstNode @object, string field, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);


/// <summary>

///     下标访问表达式


/// </summary>
public sealed record RustIndex(RustAstNode @object, RustAstNode index, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);


/// <summary>

///     类型转换表达式 (as)


/// </summary>
public sealed record RustCast(RustAstNode expression, RustAstNode type, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);


/// <summary>

///     范围表达式


/// </summary>
public sealed record RustRange(RustAstNode? start, RustAstNode? end, bool inclusive, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);


/// <summary>

///     闭包表达式


/// </summary>
public sealed record RustClosure(IReadOnlyList<string> parameters, RustAstNode body, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);


/// <summary>

///     if 表达式


/// </summary>
public sealed record RustIfExpr(
    RustAstNode condition,
    RustAstNode then_branch,
    RustAstNode? else_branch,
    TextSpan Span = default(TextSpan))
    : RustAstNode(Span);


/// <summary>

///     match 表达式


/// </summary>
public sealed record RustMatchExpr(
    RustAstNode scrutinee,
    IReadOnlyList<RustMatchArm> arms,
    TextSpan Span = default(TextSpan))
    : RustAstNode(Span);


/// <summary>

///     match 分支


/// </summary>
public sealed record RustMatchArm(RustAstNode pattern, RustAstNode body, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);


/// <summary>

///     数组表达式


/// </summary>
public sealed record RustArrayExpr(IReadOnlyList<RustAstNode> elements, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);


/// <summary>

///     元组表达式


/// </summary>
public sealed record RustTupleExpr(IReadOnlyList<RustAstNode> elements, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);


/// <summary>

///     块表达式


/// </summary>
public sealed record RustBlockExpr(IReadOnlyList<RustAstNode> statements, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);


/// <summary>

///     宏调用表达式


/// </summary>
public sealed record RustMacroCall(string name, RustAstNode body, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);

#endregion
