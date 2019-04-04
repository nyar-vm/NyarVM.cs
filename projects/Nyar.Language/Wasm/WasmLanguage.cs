namespace Nyar.Language.Wasm;

/// <summary>
///     WebAssembly 语言定义
/// </summary>
public sealed class WasmLanguage : Language
{
    public override string name => "wasm";

    public IReadOnlyList<string> extensions { get; init; } = [".wasm", ".wat"];
}