namespace Nyar.Language.Bash;

/// <summary>
///     Bash 语言定义
/// </summary>
public sealed class BashLanguage : Language
{
    public override string name => "bash";

    public IReadOnlyList<string> extensions { get; init; } = [".sh", ".bash"];
}