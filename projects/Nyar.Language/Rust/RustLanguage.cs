namespace Nyar.Language.Rust;

/// <summary>
///     Rust 语言定义
/// </summary>
public sealed class RustLanguage : Language
{
    public override string name => "rust";

    public IReadOnlyList<string> extensions { get; init; } = [".rs"];
}