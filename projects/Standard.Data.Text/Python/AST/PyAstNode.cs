using Std.Data.Text.Syntax;

namespace Std.Data.Text.Python.AST;


/// <summary>

///     Python AST 鑺傜偣鍩虹被


/// </summary>
public abstract record PyAstNode(TextSpan span = default(TextSpan));

