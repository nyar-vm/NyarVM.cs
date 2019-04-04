namespace Nyar.Language.Valkyrie.Compiler.Hir;

/// <summary>
///     `imply` 的来源。
/// </summary>
public enum HirImplySourceKind
{
    explicit_declaration,
    synthesized_default
}