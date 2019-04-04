namespace Std.Data.Text.Prolog;

public sealed class PrologLanguage : Language
{
    public override string Name => "Prolog";
    public bool DcgEnabled { get; init; } = true;
    public bool ConstraintsEnabled { get; init; }
    public bool TablingEnabled { get; init; }

    public static PrologLanguage Default => new();
    public static PrologLanguage ISO => new();
    public static PrologLanguage SWI => new()
    {
        DcgEnabled = true,
        ConstraintsEnabled = true,
        TablingEnabled = true
    };
}
