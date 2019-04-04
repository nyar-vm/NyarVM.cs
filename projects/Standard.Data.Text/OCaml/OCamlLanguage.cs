namespace Std.Data.Text.OCaml;

public sealed class OCamlLanguage : Language
{
    public override string Name => "OCaml";
    public bool PolymorphicVariantsEnabled { get; init; } = true;
    public bool FirstClassModulesEnabled { get; init; }
    public bool EffectHandlersEnabled { get; init; }
    public bool GadtEnabled { get; init; }

    public static OCamlLanguage Default => new();
    public static OCamlLanguage OCaml5 => new()
    {
        EffectHandlersEnabled = true,
        FirstClassModulesEnabled = true,
        GadtEnabled = true
    };
}
