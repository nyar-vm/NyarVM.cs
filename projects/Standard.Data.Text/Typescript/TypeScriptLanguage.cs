namespace Std.Data.Text.Typescript;

public sealed class TypeScriptLanguage : Language
{
    public override string Name => "TypeScript";

    public bool DecoratorsEnabled { get; init; } = true;
    public bool ConstEnumsEnabled { get; init; } = true;
    public bool ImportTypeSyntaxEnabled { get; init; } = true;
    public bool JsxEnabled { get; init; } = false;

    public static TypeScriptLanguage Default => new();
    public static TypeScriptLanguage Strict => new() { DecoratorsEnabled = false, ConstEnumsEnabled = false };
}
