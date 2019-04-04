using Nyar.Types.Targets;

namespace Nyar.Language.Valkyrie.Compiler.Pipeline;

/// <summary>
///     编译目标信息。
/// </summary>
public readonly record struct CompileTargetInfo(
    CompilationTarget target,
    string canonical_triple,
    TargetHostKind host_kind,
    TargetMode target_mode = TargetMode.prod);