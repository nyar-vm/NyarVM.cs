namespace Std.Data.Text.CSharp;

public sealed class CsLanguage : Language
{
    public override string Name => "CSharp";
    public bool CSharp12Enabled { get; init; } = true;

    public static CsLanguage Default => new();
}
