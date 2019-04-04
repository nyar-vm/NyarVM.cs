namespace Std.Data.Text.Julia;

public sealed class JuliaLanguage : Std.Data.Text.Syntax.Language
{
    public override string name => "Julia";
    public bool multiple_dispatch_enabled { get; init; } = true;
    public bool macro_enabled { get; init; } = true;
    public bool coroutine_enabled { get; init; }
    public bool tabular_enabled { get; init; }

    public static JuliaLanguage @default => new();
    public static JuliaLanguage julia17 => new()
    {
        multiple_dispatch_enabled = true,
        macro_enabled = true,
        coroutine_enabled = true
    };
}
