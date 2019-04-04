using Std.Data.Text.Syntax;

namespace Std.Data.Text.Julia.AST;

public abstract record JlAstNode(TextSpan span = default(TextSpan));
