using Std.Data.Text.Syntax;

namespace Std.Data.Text.Javap;

/// <summary>
///     Javap AST 基类
/// </summary>
/// <param name="span">源码位置。</param>
public abstract record JvpAstNode(TextSpan span = default);