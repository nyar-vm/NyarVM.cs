using Std.Data.Text.Syntax;

namespace Std.Data.Text.Python.AST;

#region 表达式


/// <summary>

///     二元运算表达式


/// </summary>
public sealed record PyBinaryOp(PyAstNode left, string @operator, PyAstNode right, TextSpan Span = default(TextSpan))
    : PyAstNode(Span);


/// <summary>

///     一元运算表达式


/// </summary>
public sealed record PyUnaryOp(string @operator, PyAstNode operand, TextSpan Span = default(TextSpan))
    : PyAstNode(Span);


/// <summary>

///     字面量表达式


/// </summary>
public sealed record PyLiteral(string kind, string value, TextSpan Span = default(TextSpan))
    : PyAstNode(Span);


/// <summary>

///     标识符表达式


/// </summary>
public sealed record PyIdentifier(string name, TextSpan Span = default(TextSpan))
    : PyAstNode(Span);


/// <summary>

///     函数调用表达式


/// </summary>
public sealed record PyCall(PyAstNode function, IReadOnlyList<PyAstNode> arguments, TextSpan Span = default(TextSpan))
    : PyAstNode(Span);


/// <summary>

///     属性访问表达式


/// </summary>
public sealed record PyAttribute(PyAstNode @object, string name, TextSpan Span = default(TextSpan))
    : PyAstNode(Span);


/// <summary>

///     下标访问表达式


/// </summary>
public sealed record PySubscript(PyAstNode @object, PyAstNode index, TextSpan Span = default(TextSpan))
    : PyAstNode(Span);


/// <summary>

///     列表字面量


/// </summary>
public sealed record PyList(IReadOnlyList<PyAstNode> elements, TextSpan Span = default(TextSpan))
    : PyAstNode(Span);


/// <summary>

///     元组字面量


/// </summary>
public sealed record PyTuple(IReadOnlyList<PyAstNode> elements, TextSpan Span = default(TextSpan))
    : PyAstNode(Span);


/// <summary>

///     字典字面量


/// </summary>
public sealed record PyDict(IReadOnlyList<(PyAstNode Key, PyAstNode Value)> items, TextSpan Span = default(TextSpan))
    : PyAstNode(Span);


/// <summary>

///     Lambda 表达式


/// </summary>
public sealed record PyLambda(IReadOnlyList<string> parameters, PyAstNode body, TextSpan Span = default(TextSpan))
    : PyAstNode(Span);

#endregion
