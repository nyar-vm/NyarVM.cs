namespace Nyar.Language.Valkyrie.Compiler.Meta;

/// <summary>
///     AST 片段值（代码引用）。
/// </summary>
public sealed record MetaAstValue(CompilationUnit ast) : MetaValue;