using Nyar.Types.Targets;

namespace Nyar.Language.Valkyrie.Compiler.Pipeline;

/// <summary>
///     编译计划。
///     围绕模块名、`CanonicalTriple` 与输出选项收拢编译输入。
/// </summary>
public sealed record BuildPlan(
    string module_name,
    string canonical_triple,
    string? file_path = null,
    bool enable_debug_artifacts = false,
    int optimization_level = 0,
    TargetMode target_mode = TargetMode.prod,
    string? preferred_logical_entry = null,
    BuildTargetOptions? build_options = null
);
