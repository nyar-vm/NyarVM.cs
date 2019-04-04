using Std.Data.Text.Syntax;

namespace Std.Data.Text.Typescript.AST;


/// <summary>

/// TypeScript AST 节点的抽象基类

/// </summary>
public abstract record TsAstNode(TextSpan span = default(TextSpan));

