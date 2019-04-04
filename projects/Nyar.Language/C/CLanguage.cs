namespace Nyar.Language.C;

/// <summary>
///     C 语言定义
/// </summary>
public sealed class CLanguage : Language
{
    public override string name => "c";

    public IReadOnlyList<string> extensions { get; init; } = [".c", ".h"];
}