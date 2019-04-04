namespace Std.Data.Text.Haskell;

public sealed class HaskellLanguage : Language
{
    public override string Name => "Haskell";
    public bool GadtEnabled { get; init; } = true;
    public bool TypeFamiliesEnabled { get; init; }
    public bool TemplateHaskellEnabled { get; init; }
    public bool RankNTypesEnabled { get; init; }
    public bool LambdaCaseEnabled { get; init; }

    public static HaskellLanguage Default => new();
    public static HaskellLanguage Haskell2010 => new();
    public static HaskellLanguage GHC2021 => new()
    {
        GadtEnabled = true,
        TypeFamiliesEnabled = true,
        RankNTypesEnabled = true,
        LambdaCaseEnabled = true
    };
}
