namespace Nyar.Language.PowerShell;

/// <summary>
///     PowerShell 语言定义
/// </summary>
public sealed class PowerShellLanguage : Language
{
    public override string name => "powershell";

    public IReadOnlyList<string> extensions { get; init; } = [".ps1", ".psm1", ".psd1"];
}