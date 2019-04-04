using Nyar.Analyzer.Semantic;
using Nyar.Language.Valkyrie.Compiler.Hir._Def;
using Nyar.Language.Valkyrie.Semantic;

namespace Nyar.Language.Valkyrie.Compiler.Hir;

internal sealed record HirResolvedTraitCandidate(
    SemanticNamePath trait_name,
    HirMethod target_method,
    HirDispatchKind dispatch);
