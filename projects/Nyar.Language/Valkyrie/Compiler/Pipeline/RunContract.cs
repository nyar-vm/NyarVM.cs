namespace Nyar.Language.Valkyrie.Compiler.Pipeline;

/// <summary>
///     宿主运行契约。
/// </summary>
public sealed record RunContract(
    string logical_entry,
    string physical_entry,
    string invocation_shape,
    string validation_command);