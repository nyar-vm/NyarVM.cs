namespace Nyar.Language.Valkyrie.Compiler.Mir;

/// <summary>
///     MIR 侧的 witness 分派绑定。
/// </summary>
public sealed record MirWitnessDispatchBinding(
    string trait_name,
    int slot_index,
    string method_name,
    string target_type_name,
    string implementation_function_name
);