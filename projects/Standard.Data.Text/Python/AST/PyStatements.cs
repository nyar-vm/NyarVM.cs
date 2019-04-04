using Std.Data.Text.Syntax;

namespace Std.Data.Text.Python.AST;

#region 语句


/// <summary>

///     赋值语句


/// </summary>
public sealed record PyAssign(PyAstNode target, PyAstNode value, TextSpan Span = default(TextSpan))
    : PyAstNode(Span);


/// <summary>

///     增量赋值语句（如 x += 1）


/// </summary>
public sealed record PyAugAssign(PyAstNode target, string @operator, PyAstNode value, TextSpan Span = default(TextSpan))
    : PyAstNode(Span);


/// <summary>

///     表达式语句


/// </summary>
public sealed record PyExprStmt(PyAstNode expression, TextSpan Span = default(TextSpan))
    : PyAstNode(Span);


/// <summary>

///     If 语句


/// </summary>
public sealed record PyIf(
    PyAstNode condition,
    IReadOnlyList<PyAstNode> then_body,
    IReadOnlyList<PyAstNode>? else_body,
    TextSpan Span = default(TextSpan))
    : PyAstNode(Span);


/// <summary>

///     While 循环语句


/// </summary>
public sealed record PyWhile(
    PyAstNode condition,
    IReadOnlyList<PyAstNode> body,
    TextSpan Span = default(TextSpan))
    : PyAstNode(Span);


/// <summary>

///     For 循环语句


/// </summary>
public sealed record PyFor(
    string iterator,
    PyAstNode iterable,
    IReadOnlyList<PyAstNode> body,
    TextSpan Span = default(TextSpan))
    : PyAstNode(Span);


/// <summary>

///     Return 语句


/// </summary>
public sealed record PyReturn(PyAstNode? value, TextSpan Span = default(TextSpan))
    : PyAstNode(Span);


/// <summary>

///     Yield 语句（生成器）


/// </summary>
public sealed record PyYield(PyAstNode? value, TextSpan Span = default(TextSpan))
    : PyAstNode(Span);


/// <summary>

///     Break 语句


/// </summary>
public sealed record PyBreak(TextSpan Span = default(TextSpan))
    : PyAstNode(Span);


/// <summary>

///     Continue 语句


/// </summary>
public sealed record PyContinue(TextSpan Span = default(TextSpan))
    : PyAstNode(Span);


/// <summary>

///     函数定义


/// </summary>
public sealed record PyFunctionDef(
    string name,
    IReadOnlyList<string> parameters,
    IReadOnlyList<PyAstNode> body,
    TextSpan Span = default(TextSpan))
    : PyAstNode(Span);


/// <summary>

///     类定义


/// </summary>
public sealed record PyClassDef(
    string name,
    IReadOnlyList<PyAstNode> body,
    TextSpan Span = default(TextSpan))
    : PyAstNode(Span);


/// <summary>

///     Try/Except 异常处理语句


/// </summary>
public sealed record PyTry(
    IReadOnlyList<PyAstNode> body,
    IReadOnlyList<PyExceptClause> handlers,
    IReadOnlyList<PyAstNode>? else_body,
    IReadOnlyList<PyAstNode>? finally_body,
    TextSpan Span = default(TextSpan))
    : PyAstNode(Span);


/// <summary>

///     Except 子句


/// </summary>
public sealed record PyExceptClause(
    PyAstNode? exception_type,
    string? name,
    IReadOnlyList<PyAstNode> body,
    TextSpan Span = default(TextSpan))
    : PyAstNode(Span);


/// <summary>

///     Raise 语句


/// </summary>
public sealed record PyRaise(PyAstNode? exception, TextSpan Span = default(TextSpan))
    : PyAstNode(Span);


/// <summary>

///     Import 语句


/// </summary>
public sealed record PyImport(IReadOnlyList<PyImportItem> items, TextSpan Span = default(TextSpan))
    : PyAstNode(Span);


/// <summary>

///     From...Import 语句


/// </summary>
public sealed record PyFromImport(string module, IReadOnlyList<PyImportItem> items, TextSpan Span = default(TextSpan))
    : PyAstNode(Span);


/// <summary>

///     Import 项


/// </summary>
public sealed record PyImportItem(string name, string? alias, TextSpan Span = default(TextSpan))
    : PyAstNode(Span);


/// <summary>

///     Pass 语句


/// </summary>
public sealed record PyPass(TextSpan Span = default(TextSpan))
    : PyAstNode(Span);


/// <summary>

///     模块（根节点）


/// </summary>
public sealed record PyModule(IReadOnlyList<PyAstNode> body, TextSpan Span = default(TextSpan))
    : PyAstNode(Span);

#endregion
