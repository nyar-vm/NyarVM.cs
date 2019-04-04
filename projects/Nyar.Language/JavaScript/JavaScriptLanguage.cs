namespace Nyar.Language.JavaScript;

/// <summary>
///     JavaScript 语言定义
/// </summary>
public sealed class JavaScriptLanguage : Language
{
    public override string name => "javascript";

    public IReadOnlyList<string> extensions { get; init; } = [".js", ".mjs", ".cjs"];
}