using Std.Data.Text.Syntax;

namespace Std.Data.Text.C;


/// <summary>

///     C 语言 AST 节点基类


/// </summary>
public abstract record CAstNode(TextSpan span);