namespace Nyar.Language.TypeScript;

/// <summary>
///     TypeScript 语言定义
/// </summary>
public sealed class TypeScriptLanguage : Language
{
    public override string name => "typescript";

    public IReadOnlyList<string> extensions { get; init; } = [".ts", ".tsx", ".mts", ".cts"];
}